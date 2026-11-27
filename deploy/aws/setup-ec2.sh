#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

ENV_FILE=/etc/dantophong/server.env
CERT_FILE=/etc/dantophong/server.crt
PFX_FILE=/etc/dantophong/server.pfx
APP_DIR=/opt/dantophong
SERVICE_FILE=/etc/systemd/system/dantophong.service

usage() {
    echo "Usage: sudo bash setup-ec2.sh --certificate-host <DNS-name-or-IP> [--rotate-certificate]" >&2
    exit 2
}

CERT_HOST=
ROTATE_CERTIFICATE=false
while (($#)); do
    case "$1" in
        --certificate-host)
            (($# >= 2)) || usage
            CERT_HOST=$2
            shift 2
            ;;
        --rotate-certificate)
            ROTATE_CERTIFICATE=true
            shift
            ;;
        *)
            usage
            ;;
    esac
done
[[ -n "$CERT_HOST" ]] || usage
[[ "$(id -u)" -eq 0 ]] || { echo "Run this script with sudo/root." >&2; exit 1; }

command -v openssl >/dev/null || { echo "OpenSSL is required; install it through the approved OS process." >&2; exit 1; }
command -v systemctl >/dev/null || { echo "systemd is required." >&2; exit 1; }
command -v dotnet >/dev/null || { echo ".NET 10 runtime is required." >&2; exit 1; }
command -v ss >/dev/null || { echo "The iproute2 ss command is required." >&2; exit 1; }
[[ -x /usr/bin/dotnet ]] || { echo "Expected framework-dependent .NET at /usr/bin/dotnet." >&2; exit 1; }
dotnet --list-runtimes | grep -Eq '^Microsoft\.NETCore\.App 10\.' || {
    echo "Microsoft.NETCore.App 10 runtime is not installed." >&2
    exit 1
}
getent passwd ubuntu >/dev/null || { echo "The ubuntu service account is missing." >&2; exit 1; }
getent group ubuntu >/dev/null || { echo "The ubuntu service group is missing." >&2; exit 1; }
[[ -f "$(dirname "$0")/dantophong.service" ]] || {
    echo "Copy dantophong.service beside this script before running it." >&2
    exit 1
}
[[ -f "$APP_DIR/DanToPhong.Server.dll" ]] || {
    echo "Publish the Server to $APP_DIR before running this script." >&2
    exit 1
}
[[ -f "$ENV_FILE" ]] || {
    echo "Create $ENV_FILE from the example and configure its database/SMTP values first." >&2
    exit 1
}

mkdir -p /etc/dantophong
chown root:root "$ENV_FILE"
chmod 0600 "$ENV_FILE"

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

cleanup() {
    if [[ -n "${ENV_TMP:-}" && -f "$ENV_TMP" ]]; then
        rm -f -- "$ENV_TMP"
    fi
    if [[ -n "${CERT_TMP:-}" && -d "$CERT_TMP" ]]; then
        rm -rf -- "$CERT_TMP"
    fi
}
ENV_TMP=
CERT_TMP=
trap cleanup EXIT

DB_CONNECTION=$(read_env_value DANTOPHONG_DB_CONNECTION)
if [[ -z "$DB_CONNECTION" || "$DB_CONNECTION" == *'<'* ]]; then
    echo "Configure DANTOPHONG_DB_CONNECTION in $ENV_FILE; placeholders are not accepted." >&2
    exit 1
fi

PORT_VALUE=$(read_env_value DANTOPHONG_PORT)
if [[ -z "$PORT_VALUE" ]]; then
    write_env_value DANTOPHONG_PORT 9000
elif [[ "$PORT_VALUE" != 9000 ]]; then
    echo "DANTOPHONG_PORT must be 9000 for this deployment." >&2
    exit 1
fi

TLS_VALUE=$(read_env_value DANTOPHONG_USE_TLS)
if [[ -z "$TLS_VALUE" ]]; then
    write_env_value DANTOPHONG_USE_TLS true
elif [[ "${TLS_VALUE,,}" != true ]]; then
    echo "DANTOPHONG_USE_TLS must be true; plaintext deployment is refused." >&2
    exit 1
fi

LISTEN_VALUE=$(read_env_value DANTOPHONG_LISTEN_ADDRESS)
if [[ -z "$LISTEN_VALUE" ]]; then
    write_env_value DANTOPHONG_LISTEN_ADDRESS 0.0.0.0
elif [[ "$LISTEN_VALUE" != 0.0.0.0 ]]; then
    echo "DANTOPHONG_LISTEN_ADDRESS must be blank or 0.0.0.0 for the public TLS listener." >&2
    exit 1
fi

SECONDARY_PORT=$(read_env_value DANTOPHONG_SECONDARY_PORT)
if [[ -n "$SECONDARY_PORT" ]]; then
    echo "Clear DANTOPHONG_SECONDARY_PORT; this deployment exposes only TCP 9000." >&2
    exit 1
fi
write_env_value DANTOPHONG_CERT_PATH "$PFX_FILE"

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

certificate_matches_host() {
    if [[ "$IS_IP" == true ]]; then
        openssl x509 -in "$CERT_FILE" -noout -checkip "$CERT_HOST" >/dev/null 2>&1
    else
        openssl x509 -in "$CERT_FILE" -noout -checkhost "$CERT_HOST" >/dev/null 2>&1
    fi
}

GENERATE_CERTIFICATE=false
if [[ -f "$CERT_FILE" && -f "$PFX_FILE" ]]; then
    if certificate_matches_host && openssl x509 -in "$CERT_FILE" -noout -checkend 0 >/dev/null 2>&1; then
        :
    elif [[ "$ROTATE_CERTIFICATE" == true ]]; then
        GENERATE_CERTIFICATE=true
    else
        echo "Existing certificate is expired or does not match this host; rerun with --rotate-certificate to replace it." >&2
        exit 1
    fi
elif [[ -e "$CERT_FILE" || -e "$PFX_FILE" ]]; then
    if [[ "$ROTATE_CERTIFICATE" == true ]]; then
        GENERATE_CERTIFICATE=true
    else
        echo "Only one certificate file exists; inspect it and use --rotate-certificate to replace the pair." >&2
        exit 1
    fi
else
    GENERATE_CERTIFICATE=true
fi

if [[ "$GENERATE_CERTIFICATE" == true ]]; then
    CERT_PASSWORD=$(openssl rand -hex 32)
    write_env_value DANTOPHONG_CERT_PASSWORD "$CERT_PASSWORD"
    export DANTOPHONG_CERT_PASSWORD="$CERT_PASSWORD"
    CERT_TMP=$(mktemp -d /tmp/dantophong-cert.XXXXXX)
    chmod 0700 "$CERT_TMP"
    openssl req -x509 -newkey rsa:3072 -sha256 -nodes -days 365 \
        -keyout "$CERT_TMP/server.key" \
        -out "$CERT_TMP/server.crt" \
        -subj "/CN=dantophong-server" \
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
    install -o root -g ubuntu -m 0640 "$CERT_TMP/server.pfx" "$PFX_FILE"
    install -o root -g root -m 0644 "$CERT_TMP/server.crt" "$CERT_FILE"
else
    CERT_PASSWORD=$(read_env_value DANTOPHONG_CERT_PASSWORD)
    if [[ -z "$CERT_PASSWORD" ]]; then
        echo "DANTOPHONG_CERT_PASSWORD is empty while a certificate already exists; preserve or rotate the certificate explicitly." >&2
        exit 1
    fi
    export DANTOPHONG_CERT_PASSWORD="$CERT_PASSWORD"
    chown root:ubuntu "$PFX_FILE"
    chmod 0640 "$PFX_FILE"
fi

THUMBPRINT=$(openssl x509 -in "$CERT_FILE" -noout -fingerprint -sha256 | sed 's/.*=//;s/://g' | tr '[:lower:]' '[:upper:]')
[[ "$THUMBPRINT" =~ ^[A-F0-9]{64}$ ]] || {
    echo "Could not derive the SHA-256 certificate thumbprint." >&2
    exit 1
}
if ! PFX_THUMBPRINT=$(openssl pkcs12 -in "$PFX_FILE" -clcerts -nokeys \
    -passin env:DANTOPHONG_CERT_PASSWORD 2>/dev/null |
    openssl x509 -noout -fingerprint -sha256 |
    sed 's/.*=//;s/://g' | tr '[:lower:]' '[:upper:]'); then
    echo "Could not verify the PFX; check its configured password without printing it." >&2
    exit 1
fi
if [[ "$PFX_THUMBPRINT" != "$THUMBPRINT" ]]; then
    echo "The PFX certificate does not match server.crt or its password is invalid." >&2
    exit 1
fi

chown root:root "$ENV_FILE"
chmod 0600 "$ENV_FILE"
install -o root -g root -m 0644 "$(dirname "$0")/dantophong.service" "$SERVICE_FILE"
systemctl daemon-reload
systemctl enable dantophong.service >/dev/null
if ! systemctl restart dantophong.service; then
    echo "Service restart failed; inspect with: sudo journalctl -u dantophong -n 80 --no-pager" >&2
    exit 1
fi
if ! systemctl is-active --quiet dantophong.service; then
    echo "Service is not active; inspect its journal before retrying." >&2
    exit 1
fi
if ! ss -ltnH 'sport = :9000' | grep -q .; then
    echo "Service is active but no TCP listener is visible on port 9000." >&2
    exit 1
fi

echo "Service enabled and active; TCP 9000 is listening with TLS."
echo "Certificate SAN host: $CERT_HOST"
echo "SHA-256 certificate thumbprint: $THUMBPRINT"
echo "Client server.json host must exactly match the SAN host above."
