# Work log

## 2026-11-27 — TCP-only cleanup and verification

| Work | Result | Commands / checks |
|---|---|---|
| Remove third-party networking assets and serialized references | Removed bundled networking assets, four scene components, the scooter prefab component, scripting defines, and stale ignored generated project files. Tracked-source and scene-ID scans found no remaining references. | Tracked-source search; Unity asset GUID/fileID scans |
| Align documentation/configuration | Updated protocol, architecture, requirements checklist, report, README, evidence status, diagrams and client example. Recorded gameplay as incomplete where server broadcast/track authority/reconnect are absent. | Draw.io XML parse; client example JSON parse |
| Fix Unity compilation issues and unused state | Qualified `UnityEngine.Random`; removed write-only game-result flags and obsolete host-client comment. | Unity Editor build |
| Restrict local test harnesses | TCP load/malformed-frame and login-lockout scripts reject non-loopback hosts before connecting; both guard tests passed without network access. | Script inspection; loopback guard tests |
| Verify server, Launcher and framing | Server Release build passed with 0 warnings/errors. Launcher publish passed with 6 existing generated-Designer `CS8669` warnings. Protocol framing smoke test passed. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore`; `dotnet restore .\UI\UI\UI.csproj -r win-x64`; `dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false --no-restore -o D:\DanToPhong\LauncherRelease`; `dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release` |
| Verify Unity Windows x64 | Build succeeded; no C# compiler diagnostics in final run. Unity emitted non-fatal licensing/package HTTPS messages. Player was not launched. | Unity 6000.3.28f1 `-batchmode -nographics -quit -executeMethod BuildWindowsPlayer.BuildWindows64Release` |
| Safety and Git checks | Secret-pattern scan found no credential values; staged changes are limited to the TCP-only cleanup, related code/docs, and removal of third-party networking assets. No AWS commands or Git push were run. | `git diff --check`; `git diff --cached --check`; `git grep`; credential-pattern scan; `git status --short` |

Two-client gameplay, TLS runtime cases, SMTP/DB runtime, load, DPI, packet capture,
and Internet tests remain unverified; see `docs/evidence/README.md`.

## 2026-11-27 — Final report and diagram alignment

| Work | Result | Commands / checks |
|---|---|---|
| Complete deployment and verification report | Updated the report for the dedicated TCP gameplay stream, token environment handoff, bounded client snapshot queue, current build evidence, AWS cost caveats, blocked TC statuses, owner-fill placeholders, tool-assistance placeholder, assignment crosswalk, and appendix. No unsupported runtime PASS was added. | Local Markdown link validation; PowerShell parser on all four report snippets |
| Align protocol and diagrams | Corrected TLS-default/fail-closed and gameplay-push descriptions across architecture/protocol/checklist and three Draw.io diagrams. All four Draw.io files parse as XML. No Draw.io executable was installed; PNG export remains BLOCKED. | XML parse of `docs/diagrams/*.drawio`; `winget search --id JGraph.Draw --exact --source winget` |
| Reverify .NET deliverables | Server Release build passed with 0 warnings/errors; protocol smoke passed including event-before-snapshot wire order; Launcher win-x64 publish passed. The generated `BuildLauncherRelease` directory was removed because it was not ignored; README now uses the existing ignored `LauncherRelease` output path. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore`; `dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release --no-restore`; `dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false --no-restore -o .\LauncherRelease` |
| Build from a clean clone | Commit `82b1f5f` cloned locally; Server Release, protocol smoke, Launcher win-x64 publish and Unity Windows x64 build all passed. Unity's first cold import had a package-cache rename error; a concurrent launch was rejected while the retry still held the project, then the active import completed and its log recorded `Build Successful`; player and `Assembly-CSharp.dll` were verified. Sanitized evidence is in `docs/evidence/TC08-clean-clone-build.log`; temporary clone removed after verification. | README build commands; Unity `BuildWindowsPlayer.BuildWindows64Release` |
| Record local blockers and safe next actions | SQL Server service start is permission-blocked; Docker engine is unavailable. Runtime TLS/OTP/limiter/gameplay/load/DPI tests remain BLOCKED. Documented the admin action and test-only smtp4dev setup; did not change system configuration, install Draw.io, or run AWS commands. | `Get-Service`; `docker info`; loopback TCP/1433 probe; `Start-Service MSSQL$SQLNEW` denied |

No AWS mutation, Git push, or Git identity/configuration change was performed.

## 2026-11-27 — Continuation baseline

| Work | Result | Commands / checks |
|---|---|---|
| Verify checkout and server build | `feature/own-netcode` was clean at WIP commit `ba62868`; Server Release build passed with 0 warnings/errors. The shorthand `dotnet build Server` does not identify a project in this checkout; building the actual project path succeeded. | `git status --short --branch`; `git diff --stat`; `git log -5 --oneline`; `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore` |
| Check isolated local test prerequisites | `DANTOPHONG_TEST_SA_PASSWORD` is present (value not displayed); Docker is available and `dtp-sql` is running. Existing `dtp-smtp` container is stopped; no SMTP test was run. | Environment-variable presence check; `docker info`; `docker ps -a --filter name=dtp-smtp` |
| Recover existing OTP delivery evidence | Started the already-provisioned `dtp-smtp` container. Its inbox shows the prior registration-code messages (including the user-identified 09:47–09:48 messages); this is TC04 delivery-only evidence. No email was resent and no OTP value was recorded; valid/invalid/expired entry and attempt-limit behavior remain unverified. | `docker start dtp-smtp`; loopback ports 1433/2525/5000 reachable; smtp4dev inbox UI |
| Verify TLS fail-closed runtime cases | TC05 passed all four local cases: TLS/TLS, plaintext-to-TLS rejection, mismatched certificate-pin rejection and explicit plaintext mode. Server created/used only `DanToPhongDB_Test`; TLS certificate remains outside the repository and was not imported into a system store. | Live Server; four framed-TCP/TLS probes; [TC05-local-tls-fail-closed.log](evidence/TC05-local-tls-fail-closed.log) |
| Review and replace the pre-existing capture | A broad field-name scan matched a serialized null `token` field; follow-up value-level checks found no non-empty token/password/OTP. As a precaution, revoked all six active sessions in `DanToPhongDB_Test`. Replaced the trace with an anonymous PING capture. | `tshark` offline scan (payload suppressed); SQL update restricted to `DanToPhongDB_Test` |
| Verify packet-capture cases | TC06, TC11 and TC12 passed locally on Npcap Loopback: SYN/FIN, TLS 1.3 cipher metadata and plaintext-vs-encrypted visibility were verified. The replacement trace contains only anonymous probes and passed credential-pattern checks. | `tshark` protocol/flag analysis; [TC06-TC11-TC12-loopback-capture.log](evidence/TC06-TC11-TC12-loopback-capture.log) |

## 2026-11-27 — Server-pushed TCP gameplay stream

| Work | Result | Commands / checks |
|---|---|---|
| Add a dedicated gameplay state stream | Added authenticated `GAME_STREAM` on a second TCP connection. The server pushes timestamped snapshots at a nominal 20 Hz and sends accepted game events as separate ordered TCP frames. Unity keeps at most eight snapshots, drops the oldest when full, rejects stale player sequences, interpolates, and retries the stream while its authenticated session remains available. | Server Release build; Unity Windows x64 build; source review |
| Extend protocol smoke coverage | Added a serialized `GAME_EVENT` then `GAME_SNAPSHOT` framing check, including event ID, server sequence, timestamps, and frame order. | `dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release --no-restore` |
| Rebuild Windows artifacts | Server Release succeeded with 0 warnings/errors. Launcher win-x64 publish succeeded with 6 existing generated-Designer `CS8669` warnings. Unity 6000.3.28f1 reported `Build Successful`; latest `Assembly-CSharp.dll` was rebuilt with no C# diagnostics. Two game windows were observed simultaneously at the main menu; no match was started. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore`; `dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false --no-restore`; Unity `BuildWindowsPlayer.BuildWindows64Release`; GUI observation |
| Check local runtime prerequisites | SQL Server services are installed but starting the selected local instance was denied by service permissions; Docker CLI cannot reach a running engine. No local SQL endpoint was available on loopback:1433. Consequently database-backed login/TLS/OTP, live gameplay, load, packet capture, and DPI remain BLOCKED. No AWS commands or Git push were run. | `Start-Service MSSQL$SQLNEW` (service access denied); `docker info`; loopback TCP probe |
| Keep evidence claims bounded | Recorded GUI-only evidence separately from full TC02; updated TC09 for the new protocol frame contract. No PASS was assigned to tests requiring a live server. | `docs/evidence/TC02-two-client-gui.log`; `docs/evidence/TC09-framing-smoke.log`; `docs/evidence/README.md` |

## 2026-11-27 — Local GUI test availability

| Work | Result | Commands / checks |
|---|---|---|
| Inspect local Launcher availability | `UI.exe` remained responsive, but the Computer Use window inventory did not expose a Launcher window. Did not launch a duplicate instance or submit any UI actions; TC03/TC04/TC07 therefore remain unverified. `LauncherRelease/server.json` is ignored by Git. | `Get-Process -Name UI,DanToPhong`; Computer Use `listWindows()`; `git check-ignore -v .\LauncherRelease\server.json` |

## 2026-11-27 — Evidence and documentation follow-up

| Work | Result | Commands / checks |
|---|---|---|
| Run bounded TC10 loopback checks | On the loopback-only plaintext test listener, 50 anonymous PING clients received valid framed responses. Fragmented send, partial-header disconnect/recovery, malformed JSON rejection and oversized-length rejection passed. This is a partial resilience check, not authenticated/gameplay load or a latency/capacity measurement. | `Get-NetTCPConnection -LocalPort 9000 -State Listen`; bounded PowerShell TCP harness; [TC10-loopback-framing-test.log](evidence/TC10-loopback-framing-test.log) |
| Locate and attempt Clumsy impairment | Found the existing `clumsy.exe` WinGet link and launched it once through Computer Use. No Clumsy window was exposed; no impairment was enabled and TC10 remains partial. | PATH lookup; Computer Use `startApp()` / `listWindows()` |
| Export and review Draw.io diagrams | Exported all four `.drawio` sources to PNG with the already-installed Draw.io Desktop CLI. Updated architecture and user-story labels to match the current prototype and visible test limits. | `draw.io.exe --export --format png --output ...`; visual review of all four PNGs |
| Align project documentation with observed status | Updated README, protocol, requirements checklist, report and evidence index. Recorded TC05/06/11/12 PASS, TC10 PARTIAL, and remaining OTP/gameplay/load blockers without claiming a live match. No AWS action or Git push. | Documentation/evidence cross-check; `git diff --check` |

## 2026-11-27 — Clean-checkout .NET verification

| Work | Result | Commands / checks |
|---|---|---|
| Verify committed .NET deliverables from a clean worktree | At commit `6916ac9`, Server Release build passed with 0 warnings/errors; protocol smoke passed fragmentation/coalescing, EOF, invalid/oversized frames, malformed JSON, truncated frames, oversized output, and ordered game-event/snapshot framing. Launcher win-x64 publish passed with 6 existing generated-Designer `CS8669` warnings. The temporary worktree was removed after verification. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release`; `dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release`; `dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false -o .\LauncherRelease` |
