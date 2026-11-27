# Dân Tổ Phóng

Unity scooter-racing game with a WinForms C# Launcher, a C# TCP service on
port 9000, and SQL Server. The client-to-server TCP transport and application
protocol are written by this team. MailKit is used only by the Server for
outbound email to SMTP and by the SMTP smoke-test sample; it is not used for
client-to-server networking.

The canonical submission report is
[BAO_CAO_DO_AN_NHAP_MON_MANG.md](BAO_CAO_DO_AN_NHAP_MON_MANG.md).

## Contents

1. [Architecture and current limits](#architecture-and-current-limits)
2. [Local prerequisites and configuration](#local-prerequisites-and-configuration)
3. [Local test](#local-test)
4. [Two GUI clients on one machine](#run-two-gui-clients-on-one-machine)
5. [Build and run](#build-and-run)
6. [AWS deployment notes](#aws-deployment-notes)
7. [SMTP and OTP](#smtp-and-otp)
8. [Security and verification](#security-and-verification)

## Architecture and current limits

```text
WinForms Launcher ── TCP/TLS 9000 ── .NET Server ── SQL Server / RDS
        │                                  └── SMTP for OTP
        └── launches Unity

Unity Solo: local physics
Unity multiplayer: TCP/TLS 9000 input requests + dedicated event/snapshot stream
```

The TCP framing is a 4-byte big-endian length followed by UTF-8 JSON. The
Launcher/server protocol supports account, OTP and application data. A partial
room/gameplay path implements room membership, 15–20 Hz input requests,
simplified server-side kinematics, configured checkpoint/finish gate checks,
and an authenticated TCP stream for ordered events and nominal 20 Hz
snapshots. The server has in-memory spectate/reconnect paths (120-second
disconnect grace) and server-side winner rewards, but these paths have not
been verified in a live match. The model is not full Unity vehicle/track
physics. A client may request its own elimination, but that request no longer
declares another player the winner or grants a winner reward; the server awards
a winner only after its finish-gate check. Elimination itself is still not
physics-authoritative, so gameplay is not release-ready. Room state is
process-local and is lost on server restart. See
[docs/protocol.md](docs/protocol.md) and
[docs/requirements-checklist.md](docs/requirements-checklist.md).

All implemented network features, including gameplay synchronization, use the
custom TCP/TLS stack. UDP is not implemented; it is only noted as a possible
future direction in [docs/protocol.md](docs/protocol.md#8-future-transport-direction-not-implemented).

Diagrams: [architecture](docs/diagrams/architecture.png),
[application flow](docs/diagrams/application-flow.png),
[GUI wireframe](docs/diagrams/gui-wireframe.png), and
[user stories](docs/diagrams/user-stories.png).

## Local prerequisites and configuration

- Windows 10/11 and the .NET SDK/Desktop Runtime required by the projects.
- Unity Editor version from `ProjectSettings/ProjectVersion.txt` to build the
  game.
- A SQL Server database for server runtime. Never expose database port 1433
  publicly.
- SMTP account/runtime configuration only if testing email OTP.

The server reads its database, SMTP, TLS and listener settings from environment
variables. Supply secret values through a local secret manager or environment;
never write real credentials to tracked files or command history. Example
variable names and placeholders are in `deploy/aws/server.env.example`.

The listener port defaults to `9000`; optional `DANTOPHONG_SECONDARY_PORT` is
disabled by default and uses the same TLS certificate. TLS is required by
default. For Internet connections, the chosen design is a self-signed server
certificate pinned by its SHA-256 leaf-certificate thumbprint in each client's
`server.json` (`certificateThumbprintSha256`). The certificate SAN must match
the configured `host`; clients retain hostname and validity checks and accept
an untrusted root only when the configured pin matches. Rotate the certificate
and pin together. The endpoint template is
[`deploy/client/server.json.example`](deploy/client/server.json.example).
TLS listens on all interfaces by default; `DANTOPHONG_LISTEN_ADDRESS` can bind
to a specific IP. Plaintext mode is always loopback-only and must be explicitly
enabled for isolated testing.

### Local test

Use the test runner for the normal local setup. It requires the already-running
Docker containers `dtp-sql` and `dtp-smtp`; it checks them and their local
ports, but does not start, stop, or modify containers. Set
`DANTOPHONG_TEST_SA_PASSWORD` in the user or current PowerShell environment.
The runner constructs `DANTOPHONG_DB_CONNECTION` for the dedicated
`DanToPhongDB_Test` database without printing it, creates a temporary
self-signed certificate outside the repository, and starts the server with TLS
bound to `127.0.0.1:19000`. It also configures SMTP4dev at `127.0.0.1:2525`.
The test Server binds to port `19000`, not `9000`.

```powershell
.\scripts\run-local.ps1
```

Script: [`scripts/run-local.ps1`](scripts/run-local.ps1).

Keep that PowerShell window open while testing and stop the server with
Ctrl+C. Registration mail and OTP messages can be viewed in SMTP4dev at
`http://localhost:5000`. Never copy the test password, generated certificate,
or connection string into the repository.

TLS can be disabled **only for isolated GUI testing** with disposable accounts
and data, never for an Internet test or Release deployment. The normal runner
above intentionally keeps TLS enabled. For the plaintext-only GUI alternative,
first verify the same two local containers and use a dedicated PowerShell
window; the server must remain loopback-bound and use the test database:

```powershell
if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
    throw 'Set DANTOPHONG_TEST_SA_PASSWORD before starting the test server.'
}
$db = New-Object System.Data.Common.DbConnectionStringBuilder
$db['Server'] = 'localhost,1433'
$db['Database'] = 'DanToPhongDB_Test'
$db['User Id'] = 'sa'
$db['Password'] = $env:DANTOPHONG_TEST_SA_PASSWORD
$db['Encrypt'] = 'True'
$db['TrustServerCertificate'] = 'True'
$env:DANTOPHONG_DB_CONNECTION = $db.ConnectionString
$env:DANTOPHONG_PORT = '19000'
$env:DANTOPHONG_LISTEN_ADDRESS = '127.0.0.1'
$env:DANTOPHONG_USE_TLS = 'false'
$env:DANTOPHONG_SMTP_MODE = 'smtp4dev'
$env:DANTOPHONG_SMTP_HOST = '127.0.0.1'
$env:DANTOPHONG_SMTP_PORT = '2525'
dotnet run --project .\UI\DanToPhong.Server\DanToPhong.Server.csproj
```

For this mode only, configure the untracked client `server.json` with
`host: "localhost"`, `port: 19000`, and `useTls: false`; do not reuse that
configuration outside the local GUI test. The test runner's TLS-pinned mode
remains preferred.

### Start the server locally

In a PowerShell session with an externally configured database connection and
certificate:

```powershell
dotnet run --project .\UI\DanToPhong.Server\DanToPhong.Server.csproj
```

### Start the Launcher

```powershell
dotnet run --project .\UI\UI\UI.csproj
```

### Run two GUI clients on one machine

This is a manual GUI test; the console evidence in
[docs/evidence/README.md](docs/evidence/README.md) does not verify Launcher or
Unity rendering and controls.

1. Follow [Local test](#local-test) and run `.\scripts\run-local.ps1` in its
   own PowerShell window. The TLS-pinned test server uses loopback port `19000`
   and directs OTP mail only to smtp4dev on port `2525`. If you use the
   plaintext-only GUI alternative instead, follow its explicit environment
   setup and keep the server bound to `127.0.0.1`.
2. Publish the Launcher as described below and copy
   `deploy\client\server.json.example` to `LauncherRelease\server.json`.
   Set `host` to `localhost` and `port` to `19000`. With the normal TLS runner,
   set `useTls` to `true` and `certificateThumbprintSha256` to the printed pin.
   For the isolated plaintext GUI alternative only, set `useTls` to `false`
   and omit the pin. Do not copy the temporary server certificate into the
   repository.
3. Place the Unity player at `LauncherRelease\Game\DanToPhong.exe` (or use the
   documented `BuildGame36` layout), then start the Launcher executable twice.
   Sign in with two different disposable test accounts. The Launcher hides
   while its Unity game is running and returns when that game exits.
4. In the first client, open Rooms and choose Create Room. In the second,
   refresh/open the room list and choose the host's room to join. Observe both
   Unity windows and the server output; do not treat a successful launch as
   proof that game state is synchronized. Use the GUI checklist in
   [docs/morning-checklist.md](docs/morning-checklist.md) to record results.
5. Close both Unity clients, then stop the server with Ctrl+C in the server
   window. Do not stop a listener you did not start.

## Build and run

### Publish the Windows Launcher

```powershell
dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false -o .\LauncherRelease
Copy-Item .\deploy\client\server.json.example .\LauncherRelease\server.json
```

Set the endpoint in the untracked `LauncherRelease\server.json`. A framework-dependent build
requires the matching .NET Desktop Runtime on the client PC.

### Build Unity

Build from the Unity Editor using the scenes in
`ProjectSettings/EditorBuildSettings.asset`. Build output belongs outside the
tracked source tree or in ignored build directories. Run two independent
instances only for a test environment with a configured server and separate
test accounts.

### Protocol and SMTP smoke tests

```powershell
dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release
```

The default run checks framing, the in-memory game mailbox, account boundaries,
and bounded email-queue retries using a simulated sender. It does not prove
SMTP delivery, TLS, database, Internet connectivity, multiplayer correctness,
or load capacity.

An optional sample sends a synthetic Vietnamese message with a text attachment
to the fixed loopback smtp4dev endpoint:

```powershell
dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release -- --smtp4dev-sample
```

This sends only to `smtp-sample@example.invalid` on `127.0.0.1:2525`, uses no
SMTP credentials, and uses MailKit's SMTP `ProtocolLogger` with its trace kept
in memory. This logger is for the server-to-SMTP sample only, not the
client-to-server protocol.
`RedactSecrets` covers authentication secrets only, not message content; the
logger must not be used with real OTP or personal data.

## AWS deployment notes

AWS resources may incur charges. Do not start/stop resources, change network
rules, or allocate/move an Elastic IP without explicit owner approval. Before
an approved test, verify current EC2, RDS, EIP and Security Group state using
read-only AWS CLI queries with the account's configured profile and region.
Preserve any Elastic IP associated with the RDS network interface. If the
owner approves a short test, start RDS and wait for `available`, then start
EC2; when finished, stop EC2 and wait for `stopped`, then stop RDS. Restrict
TCP 9000 to a verified source `/32`; never use `0.0.0.0/0`.

The systemd unit template is `deploy/aws/dantophong.service`. Configure its
environment file outside Git with restricted permissions and install/enable
the service only on an approved EC2 deployment. The one-time EC2 setup script
is [deploy/aws/setup-ec2.sh](deploy/aws/setup-ec2.sh); it creates a self-signed
certificate for the requested SAN host when needed and enables/restarts the
unit. The local [scripts/aws-test-helper.ps1](scripts/aws-test-helper.ps1)
provides read-only status, explicitly confirmed RDS-then-EC2 start and
EC2-then-RDS stop flows, plus live TLS pin verification.

The [demo runbook](docs/demo-runbook.md) documents setup, test and shutdown
steps; it is not evidence that AWS was accessed or deployed. This repository's
local test evidence does not establish an Internet deployment.

## SMTP and OTP

The server supports implicit TLS on SMTP port 465 and STARTTLS on port 587.
Runtime variables include `DANTOPHONG_SMTP_HOST`, `DANTOPHONG_SMTP_PORT`,
`DANTOPHONG_SMTP_USER`, and `DANTOPHONG_SMTP_PASSWORD`. OTP expiration,
attempt limits and resend cooldown are enforced server-side. SMTP credentials
must remain outside this repository.

## Security and verification

- Never commit database credentials, SMTP App Passwords, tokens, private keys,
  certificates, runtime secret files or real user data.
- For Internet use, enable TLS and pin the self-signed server certificate's
  SHA-256 thumbprint in client `server.json`; make sure its SAN matches the
  configured hostname. Do not disable certificate validation.
- Test SMTP, database access, TLS fail-closed behavior, reconnects, and two
  clients across independent networks before any real-account use.
- Record only observed test results. Sanitize screenshots/logs before saving
  them under `docs/evidence/`; never fabricate evidence.
- The project is not yet demonstrated as release-ready multiplayer software;
  consult the requirements checklist for incomplete gameplay/server work.

### Test status

| Test | Status | Scope |
|---|---|---|
| TC01 | PASS | Local TCP reachability observation only; not a two-network test. |
| TC02 | BLOCKED | No two clients on independent networks. |
| TC03 | BLOCKED | No live two-client GUI room/game observation. |
| TC04 | PARTIAL | SMTP delivery observed; OTP entry/expiry/attempt lockout not observed. |
| TC05 | PASS | Four pinned TLS/plaintext cases on loopback; see evidence index. |
| TC06 | PASS | Prior sanitized local capture; see evidence index. |
| TC07 | BLOCKED | Live match reconnect/restart not observed. |
| TC08 | PASS | Unity player build only; no gameplay/network proof. |
| TC09 | PASS | In-memory protocol/framing and queue smoke only. |
| TC10 | PARTIAL | Anonymous loopback framing/load probes; no authenticated gameplay or Clumsy run. |
| TC11–TC12 | PASS | Prior sanitized Npcap loopback capture; not proof of Internet behavior. |
| T1 | PASS | Loopback listener observed; does not show public reachability. |
| T2–T8 | PARTIAL | Console protocol harness passed; Launcher/Unity GUI still requires manual verification. [Latest log](docs/evidence/T2-T8-protocol-console-20261127-184734.log). |

Current evidence and test scope are indexed in
[docs/evidence/README.md](docs/evidence/README.md); the detailed requirement
mapping is in [docs/requirements-traceability.md](docs/requirements-traceability.md).
The manual capture plan is in
[docs/evidence/screenshot-checklist.md](docs/evidence/screenshot-checklist.md).
The canonical implementation, operations and test report is
[BAO_CAO_DO_AN_NHAP_MON_MANG.md](BAO_CAO_DO_AN_NHAP_MON_MANG.md).
