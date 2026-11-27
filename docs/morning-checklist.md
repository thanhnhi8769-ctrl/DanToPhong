# Morning checklist

Run these steps from the repository root on `feature/own-netcode`. Do not push,
merge, or touch AWS until you have reviewed the results and explicitly decide.

## 1. Confirm repository and builds

```powershell
git status --short --branch
git log -5 --oneline
git diff --stat
git grep -i photon
dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release
dotnet build .\UI\UI\UI.csproj -c Release
dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release
```

`git grep -i photon` should return no matches (exit code 1 means no matches).
Review the six generated Designer warnings from the Launcher build; do not
silence them as part of this verification.

## 2. Run two GUI clients locally

1. Confirm `DANTOPHONG_TEST_SA_PASSWORD` is available to your user and only
   `dtp-sql` / `dtp-smtp` are the intended local services. Do not record the
   password. The script refuses to start if the test database or smtp4dev
   loopback port is unreachable.
2. In a dedicated PowerShell window:

   ```powershell
   .\scripts\run-local.ps1
   ```

   Keep this window open. The script creates an external self-signed test
   certificate, binds TLS only to `127.0.0.1:19000`, targets
   `DanToPhongDB_Test`, and forces OTP mail to smtp4dev at `127.0.0.1:2525`.
   It prints the public endpoint values and pin; it does not print DB/SMTP
   credentials. Do not put the generated certificate or secrets in Git.
3. Publish the Launcher and prepare the Unity player:

   ```powershell
   dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false -o .\LauncherRelease
   Copy-Item .\deploy\client\server.json.example .\LauncherRelease\server.json
   ```

   Edit the ignored `LauncherRelease\server.json`: set `host` to `localhost`,
   `port` to `19000`, `useTls` to `true`, and
   `certificateThumbprintSha256` to the pin printed by the server script. Place
   the Unity player at `LauncherRelease\Game\DanToPhong.exe` or use the
   documented `BuildGame36` layout.
4. Start two Launcher processes and sign in using two different disposable
   test accounts. If registration is needed, observe delivery at
   `http://localhost:5000` and record only sanitized evidence. Try one correct,
   one incorrect, and one expired OTP; test the attempt limit without recording
   the code itself.
5. In client A choose Rooms → Create Room. In client B refresh the room list
   and join A's room. Observe both Unity windows and the Server log. Test
   snapshot updates, leaving with the window close button, reconnect within the
   grace period, spectating a running room, and a clean Server shutdown. Record
   only what actually happens; a successful login is not proof of gameplay.
6. Stop both clients. Stop the Server with Ctrl+C in its own PowerShell window.
   Verify only your test listeners are closed:

   ```powershell
   Get-NetTCPConnection -State Listen -LocalPort 19000,19001 -ErrorAction SilentlyContinue
   ```

   Port 9000 had a pre-existing listener (PID 13472); do not stop it unless you
   identify it and decide independently.

## 3. Complete evidence and report fields

- Save sanitized screenshots/logs under `docs/evidence/`; include local time,
  test ID, build, and the observed result. Never include a password, OTP,
  session token, connection string, private key, or real personal data.
- Update [docs/evidence/README.md](evidence/README.md),
  [docs/requirements-traceability.md](requirements-traceability.md), and
  [BAO_CAO_TRIEN_KHAI_VA_KIEM_THU.md](../BAO_CAO_TRIEN_KHAI_VA_KIEM_THU.md)
  to match the evidence.
- Review every `.drawio` source and PNG against the actual UI and flow. Complete
  the owner-only group/contact/tool fields using `[ĐIỀN: ...]`; do not invent
  names, IDs, contacts, or contribution claims.
- Review [docs/blocked.md](blocked.md), especially the exact external test PFX
  artifacts. Verify each path before manually removing only generated test
  files; no external cleanup was performed by this session.

## 4. Decide release and AWS work

- Review `git status`, commit list, report, evidence, and remaining limitations.
  Decide separately whether to merge into `main` and whether to `git push`.
  This session did neither.
- AWS is a separate approved task. Before any action, check current Free Tier,
  pricing, resource state, and owner approval; then follow
  [docs/demo-runbook.md](demo-runbook.md). Do not open 1433 publicly; restrict
  temporary TCP 9000 ingress to a verified `/32`, and shut down in the order
  EC2 then RDS.
