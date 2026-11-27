# Dân Tổ Phóng

Unity scooter-racing game with a WinForms C# Launcher, a C# TCP service on
port 9000, and SQL Server. The network stack is developed in this repository;
no third-party networking library is used.

## Contents

1. [Architecture and current limits](#architecture-and-current-limits)
2. [Local prerequisites and configuration](#local-prerequisites-and-configuration)
3. [Build and run](#build-and-run)
4. [AWS deployment notes](#aws-deployment-notes)
5. [SMTP and OTP](#smtp-and-otp)
6. [Security and verification](#security-and-verification)

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
a winner only after its finish-gate check. Game input uses the authenticated
player identity and server-side motion; client-supplied positions are ignored.
Elimination itself is still a client-requested forfeit rather than a server
physics result, so gameplay is not release-ready. Room state is process-local
and is lost on server restart. See
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

The listener port defaults to `9000`. TLS is required by default; configure a
certificate path/password outside the repository. Set TLS off only explicitly
for an isolated test using dummy data. Client endpoint template:
`deploy/client/server.json.example`.

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
SMTP credentials, and keeps the `ProtocolLogger` trace in memory.
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
the service only on an approved EC2 deployment.

## SMTP and OTP

The server supports implicit TLS on SMTP port 465 and STARTTLS on port 587.
Runtime variables include `DANTOPHONG_SMTP_HOST`, `DANTOPHONG_SMTP_PORT`,
`DANTOPHONG_SMTP_USER`, and `DANTOPHONG_SMTP_PASSWORD`. OTP expiration,
attempt limits and resend cooldown are enforced server-side. SMTP credentials
must remain outside this repository.

## Security and verification

- Never commit database credentials, SMTP App Passwords, tokens, private keys,
  certificates, runtime secret files or real user data.
- For Internet use, TLS must be enabled on server and clients with a trusted
  certificate matching the configured hostname.
- Test SMTP, database access, TLS fail-closed behavior, reconnects, and two
  clients across independent networks before any real-account use.
- Record only observed test results. Sanitize screenshots/logs before saving
  them under `docs/evidence/`; never fabricate evidence.
- The project is not yet demonstrated as release-ready multiplayer software;
  consult the requirements checklist for incomplete gameplay/server work.
