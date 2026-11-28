#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

ENV_FILE=/etc/dantophong/server.env

usage() {
    echo "Usage: sudo bash rotate-tls-certificate.sh --certificate-host <DNS-name-or-IP>" >&2
    exit 2
}

CERT_HOST=
while (($#)); do
    case "$1" in
        --certificate-host)
            (($# >= 2)) || usage
            CERT_HOST=$2
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done
[[ -n "$CERT_HOST" ]] || usage
[[ "$(id -u)" -eq 0 ]] || { echo "Run this script with sudo/root." >&2; exit 1; }
command -v openssl >/dev/null || { echo "OpenSSL is required." >&2; exit 1; }
command -v systemctl >/dev/null || { echo "systemd is required." >&2; exit 1; }
command -v ss >/dev/null || { echo "The iproute2 ss command is required." >&2; exit 1; }
[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE." >&2; exit 1; }

read_env_value() {
    local wanted=$1 line value=
    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            "$wanted"=*)
                value=${line#*=}
                value=${value%$'\r'}
                ;;
        esac
    done < "$ENV_FILE"
    printf '%s' "$value"
}

PFX_FILE=$(read_env_value DANTOPHONG_CERT_PATH)
CERT_PASSWORD=$(read_env_value DANTOPHONG_CERT_PASSWORD)
if [[ -z "$PFX_FILE" || "$PFX_FILE" != /etc/dantophong/*.pfx ||
      "$PFX_FILE" == *".."* ]]; then
    echo "A valid DANTOPHONG_CERT_PATH is required." >&2
    exit 1
fi
UPDATE_CERT_PASSWORD=false
if [[ -z "$CERT_PASSWORD" ]]; then
    CERT_PASSWORD=$(openssl rand -hex 32)
    UPDATE_CERT_PASSWORD=true
fi

CERT_DIR=$(dirname "$PFX_FILE")
CERT_NAME=$(basename "$PFX_FILE" .pfx)
CERT_FILE="$CERT_DIR/$CERT_NAME.crt"
KEY_FILE="$CERT_DIR/$CERT_NAME.key"
[[ -f "$PFX_FILE" && -f "$CERT_FILE" && -f "$KEY_FILE" ]] || {
    echo "The existing certificate files are incomplete; inspect them before rotating." >&2
    exit 1
}

IS_IP=false
if [[ "$CERT_HOST" == *:* || "$CERT_HOST" =~ ^[0-9]+(\.[0-9]+){3}$ ]]; then
    IS_IP=true
    SAN_VALUE="IP:$CERT_HOST"
else
    DNS_PATTERN='^([A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?)(\.([A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?))*$'
    [[ "$CERT_HOST" =~ $DNS_PATTERN ]] || {
        echo "Certificate host must be a DNS name or IP address without a wildcard." >&2
        exit 1
    }
    SAN_VALUE="DNS:$CERT_HOST"
fi

CERT_TMP=$(mktemp -d "$CERT_DIR/.rotate.XXXXXX")
chmod 0700 "$CERT_TMP"
ENV_TMP=
BACKUP_READY=false
COMMITTED=false

cleanup() {
    local status=$?
    trap - EXIT
    if [[ "$status" -ne 0 && "$BACKUP_READY" == true && "$COMMITTED" == false ]]; then
        install -o root -g root -m 0644 "$CERT_TMP/old.crt" "$CERT_FILE"
        install -o root -g root -m 0600 "$CERT_TMP/old.key" "$KEY_FILE"
        install -o root -g ubuntu -m 0640 "$CERT_TMP/old.pfx" "$PFX_FILE"
        install -o root -g root -m 0600 "$CERT_TMP/old.env" "$ENV_FILE"
        systemctl restart dantophong.service >/dev/null 2>&1 || true
    fi
    if [[ -n "$ENV_TMP" && -f "$ENV_TMP" ]]; then
        rm -f -- "$ENV_TMP"
    fi
    rm -rf -- "$CERT_TMP"
    exit "$status"
}
trap cleanup EXIT

write_env_value() {
    local wanted=$1 value=$2 line found=false
    ENV_TMP=$(mktemp /etc/dantophong/server.env.XXXXXX)
    chmod 0600 "$ENV_TMP"
    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            "$wanted"=*)
                if [[ "$found" == false ]]; then
                    printf '%s=%s\n' "$wanted" "$value" >> "$ENV_TMP"
                    found=true
                fi
                ;;
            *)
                printf '%s\n' "${line%$'\r'}" >> "$ENV_TMP"
                ;;
        esac
    done < "$ENV_FILE"
    if [[ "$found" == false ]]; then
        printf '%s=%s\n' "$wanted" "$value" >> "$ENV_TMP"
    fi
    chown root:root "$ENV_TMP"
    chmod 0600 "$ENV_TMP"
    mv -f -- "$ENV_TMP" "$ENV_FILE"
    ENV_TMP=
}

cp -p -- "$ENV_FILE" "$CERT_TMP/old.env"
cp -p -- "$CERT_FILE" "$CERT_TMP/old.crt"
cp -p -- "$KEY_FILE" "$CERT_TMP/old.key"
cp -p -- "$PFX_FILE" "$CERT_TMP/old.pfx"
BACKUP_READY=true

export DANTOPHONG_CERT_PASSWORD="$CERT_PASSWORD"
openssl req -x509 -newkey rsa:3072 -sha256 -nodes -days 365 \
    -keyout "$CERT_TMP/server.key" \
    -out "$CERT_TMP/server.crt" \
    -subj "/CN=$CERT_HOST" \
    -addext "subjectAltName=$SAN_VALUE" \
    -addext "extendedKeyUsage=serverAuth" >/dev/null 2>&1
if [[ "$IS_IP" == true ]]; then
    openssl x509 -in "$CERT_TMP/server.crt" -noout -checkip "$CERT_HOST" >/dev/null
else
    openssl x509 -in "$CERT_TMP/server.crt" -noout -checkhost "$CERT_HOST" >/dev/null
fi
openssl pkcs12 -export \
    -out "$CERT_TMP/server.pfx" \
    -inkey "$CERT_TMP/server.key" \
    -in "$CERT_TMP/server.crt" \
    -name dantophong-server \
    -passout env:DANTOPHONG_CERT_PASSWORD >/dev/null 2>&1

THUMBPRINT=$(openssl x509 -in "$CERT_TMP/server.crt" -noout -fingerprint -sha256 |
    sed 's/.*=//;s/://g' | tr '[:lower:]' '[:upper:]')
PFX_THUMBPRINT=$(openssl pkcs12 -in "$CERT_TMP/server.pfx" -clcerts -nokeys \
    -passin env:DANTOPHONG_CERT_PASSWORD 2>/dev/null |
    openssl x509 -noout -fingerprint -sha256 |
    sed 's/.*=//;s/://g' | tr '[:lower:]' '[:upper:]')
[[ "$THUMBPRINT" =~ ^[A-F0-9]{64}$ && "$PFX_THUMBPRINT" == "$THUMBPRINT" ]] || {
    echo "The generated PFX and certificate did not validate." >&2
    exit 1
}

if [[ "$UPDATE_CERT_PASSWORD" == true ]]; then
    write_env_value DANTOPHONG_CERT_PASSWORD "$CERT_PASSWORD"
fi
install -o root -g root -m 0644 "$CERT_TMP/server.crt" "$CERT_FILE"
install -o root -g root -m 0600 "$CERT_TMP/server.key" "$KEY_FILE"
install -o root -g ubuntu -m 0640 "$CERT_TMP/server.pfx" "$PFX_FILE"
systemctl restart dantophong.service
systemctl is-active --quiet dantophong.service
LISTENER_READY=false
for attempt in {1..30}; do
    if ss -ltnH 'sport = :9000' | grep -q .; then
        LISTENER_READY=true
        break
    fi
    sleep 1
done
[[ "$LISTENER_READY" == true ]] || {
    echo "Service restarted but TCP 9000 did not become ready within 30 seconds." >&2
    exit 1
}

COMMITTED=true
printf 'TLS certificate rotated; service active; TCP 9000 listening; SHA-256 pin: %s\n' "$THUMBPRINT"
