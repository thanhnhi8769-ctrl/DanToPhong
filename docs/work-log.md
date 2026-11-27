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

## 2026-11-27 — Per-client gameplay backpressure

| Work | Result | Commands / checks |
|---|---|---|
| Add bounded outbound mailboxes for game streams | Each client now has a bounded important-event channel (`Wait`) and a one-slot snapshot channel (`DropOldest`); one sender serializes frames per stream. Queue waits occur outside room locks, so a slow stream does not block another client's stream. Socket I/O errors are logged by session. | Server Release build; protocol smoke including full-event-queue wait and stale-snapshot drop |
| Record the focused smoke result and update protocol notes | Updated TC09 evidence, protocol limits and checklist. The tests are in-memory only; no live match, latency or multi-client socket result is claimed. Existing server process PID 13472 held the normal build output, so builds used ignored `obj` output paths without stopping it. | `dotnet build` Server and smoke with isolated `OutDir`; `dotnet <smoke DLL>`; `git diff --check` |

## 2026-11-27 — Rubric traceability baseline

| Work | Result | Commands / checks |
|---|---|---|
| Create requirements traceability matrix | Added `docs/requirements-traceability.md` for P0/P1/P2 criteria, source locations, evidence and explicit gaps. Test and report requirements remain partial or unverified where live evidence is absent. | Markdown relative-link validation; cross-check against `docs/evidence/README.md` and current source findings |
| Audit core R4 protections | Confirmed username/password validation exists on both launcher and server paths; user text schema is NVARCHAR and Username has a database UNIQUE constraint; PBKDF2 uses random salt and fixed-time comparison. SQL-command searches found no interpolated SQL; remaining CHAR/VARCHAR columns hold hashes or machine command IDs. No database state was changed in this audit. | Targeted source review; `rg` checks over server `.cs` files |
| Run Chapter 3 T1 | `netstat -ano | findstr 9000` observed a LISTENING socket on `127.0.0.1:9000` (PID 13472). T2–T8 remain BLOCKED because the Launcher window cannot be interacted with; no two-client behavior is inferred. | [T1-T8-chapter3-tests.log](evidence/T1-T8-chapter3-tests.log) |

## 2026-11-27 — Account validation and report follow-up

| Work | Result | Commands / checks |
|---|---|---|
| Align account input limits across client and server | Added shared validation for email length up to the database column limit (100) and password length 8–128, with matching server checks and Launcher text-box limits. No Designer-generated files were edited. | Protocol smoke: maximum email/password accepted; overlong email/password and malformed email rejected |
| Verify changed .NET projects | Server and smoke harness built with 0 warnings/errors in isolated ignored output paths; smoke passed framing, mailbox, and input-validation checks. Launcher Release build succeeded with six existing generated-Designer `CS8669` warnings. The initial `win-x64 --no-restore` build failed with `NETSDK1047` because current restore assets lack that RID; the non-RID build validated the UI code changes. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore -p:OutDir=...`; `dotnet build .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release --no-restore -p:OutDir=...`; smoke DLL; `dotnet build .\UI\UI\UI.csproj -c Release --no-restore -p:OutDir=...` |
| Align report, protocol and evidence | Added explicit protocol-version gap, corrected stale SQL/Launcher status, documented form limits and await/MailKit details, and updated TC09 evidence. No live DB or GUI validation is claimed. | `git diff --check`; relative Markdown-link validation; staged secret-pattern scan |

## 2026-11-27 — Bounded OTP email delivery

| Work | Result | Commands / checks |
|---|---|---|
| Decouple OTP requests from SMTP delivery | Added a bounded Channel queue with reservations, one background sender, three attempts for SMTP 4xx/5xx and transient transport failures, request-path queue-full response, and a 20-second graceful drain. Verification codes are hashed in DB before queue commit; logs use job IDs/status or exception type and omit recipient/code. The queue is in-memory and may lose pending jobs after a crash. | Source review of request handlers, worker, and verification state |
| Test queue limits and retry behavior | Added in-memory smoke checks with an injected sender. Capacity is enforced; simulated SMTP 451 then 550 are retried and the third attempt succeeds. The test does not connect to smtp4dev/Gmail. | `dotnet restore .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj`; isolated-output build and smoke DLL |
| Update traceability and evidence | TC09 now includes bounded queue/retry logic; E2 is PARTIAL until worker delivery and outage recovery are observed against SMTP. TC04 remains delivery-only historical evidence; no OTP UI action was possible. | Evidence/report/matrix cross-check; relative-link check; `git diff --check`; staged secret-pattern scan |

## 2026-11-27 — SMTP sample and protocol trace

| Work | Result | Commands / checks |
|---|---|---|
| Verify a synthetic MIME attachment through smtp4dev | smtp4dev accepted a Vietnamese sample with an in-memory text attachment for a fixed `.invalid` test recipient. A MailKit ProtocolLogger used `RedactSecrets=true` and kept its trace in memory. No real account, SMTP credential, OTP, or external endpoint was used. | `dotnet .\UI\DanToPhong.Protocol.Smoke\obj\verify-email\DanToPhong.Protocol.Smoke.dll --smtp4dev-sample`; SMTP4dev accepted send |
| Record E3 limitations | E3 remains PARTIAL: no SMTP AUTH exchange tested secret redaction; ProtocolLogger does not redact message bodies and is not enabled in OTP production. Added sanitized evidence and updated matrix/report. | [E3-smtp-sample.log](evidence/E3-smtp-sample.log); relative-link check; `git diff --check`; staged secret-pattern scan |

## 2026-11-27 - Preventing client elimination from awarding a win

| Work | Result | Commands / checks |
|---|---|---|
| Keep winner assignment server-authoritative | A client may still request its own elimination/forfeit, but that event no longer marks the remaining player finished or awards a win. Winner assignment remains tied to server-side finish-gate detection. | Targeted review of `TcpGameRoomRegistry.SubmitEvent` and `Simulate` |
| Verify client identity boundaries | The event handler uses the authenticated session user ID; a smoke request with forged `UserId` and `TargetId` only eliminated its caller, left the opponent unchanged and created no winner/reward state. `INVITE` intentionally queues a pending notification for its selected recipient, not a race/account-state change. | Source review of `Program.HandleGameEventAsync` and `HandleInviteAsync`; TC09 in-memory smoke |
| Record exact gameplay checks and gaps | Documented bounded input values, server speed clamp, ordered gate checks, simulation/input/disconnect timing, and the unvalidated client timestamp; explicitly states no client position is accepted and no separate displacement, collision, direction, race-duration, or fall detector exists. | `TcpGameRoomRegistry.SubmitInput`, `Simulate`, and `CrossedGate`; `git diff --check` |
| Align documentation and evidence | Updated the report's known-limitations section, README, traceability and TC09. No live match or physics-based elimination is claimed. | Relative path check; secret-pattern scan |

## 2026-11-27 - Closing the client socket with the main form

| Work | Result | Commands / checks |
|---|---|---|
| Close the TCP connection in `Main_FormClosing` | The form now unsubscribes its status handler and synchronously closes the network stream/socket before finishing shutdown. The outer application owner still disposes the service after `Application.Run` returns. | Source review of `Main_FormClosing` and `NetworkClientService.Disconnect` |
| Verify the Launcher build | Build succeeded with 0 errors and 6 existing nullable-context warnings in generated Designer files. | `dotnet build .\UI\UI\UI.csproj -c Release --no-restore -p:OutDir=.\UI\UI\obj\verify-r3g\` |
| Record remaining R3g limitation | The service `Dispose` method still waits synchronously for its heartbeat task after closing the socket; no claim is made that full async shutdown is complete. | `.Result`/`.Wait()` search plus review of `GetAwaiter().GetResult()` in service disposal |

## 2026-11-27 - Keeping per-client server I/O asynchronous

| Work | Result | Commands / checks |
|---|---|---|
| Remove unnecessary thread-pool scheduling around client I/O | The accept loop now invokes `HandleClientAsync` directly; asynchronous socket/TLS waits yield without consuming a dedicated `Task.Run` scheduling step. Per-session completion, catch/finally cleanup and capacity release are unchanged. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore -p:OutDir=.\UI\DanToPhong.Server\obj\verify-r3\`; targeted review of the accept loop |

## 2026-11-27 - Chapter 3 GUI checks follow-up

| Work | Result | Commands / checks |
|---|---|---|
| Reconfirm T1 and retry Launcher availability | T1 listener remains on loopback `127.0.0.1:9000` (PID 13472); it may be an older process and is not treated as latest-build runtime evidence. A single Computer Use launch attempt produced no targetable Launcher window; T2-T8 remain BLOCKED and no UI/network actions were submitted. | `Get-NetTCPConnection -LocalPort 9000 -State Listen`; Computer Use `startApp` once and `listWindows()` twice |

## 2026-11-27 - Aligning gameplay story and diagram with observed checks

| Work | Result | Commands / checks |
|---|---|---|
| Clarify gameplay acceptance limits in the report | The story now lists the exact owner/input/tick/speed/gate checks and states that client timestamp freshness, fall/collision physics and live gameplay are unverified. | Source review of `TcpGameRoomRegistry`; report cross-check against the known-limitations section |
| Correct the gameplay user-story diagram | Replaced the ambiguous "client elimination" label with the current self-forfeit-only and finish-gate winner behavior; exported and visually verified the matching PNG. | `draw.io.exe --export --format png --output .\\docs\\diagrams\\user-stories.png .\\docs\\diagrams\\user-stories.drawio` |

## 2026-11-27 - Verifying bounded OTP worker delivery

| Work | Result | Commands / checks |
|---|---|---|
| Verify the real OTP worker with smtp4dev | The bounded queue called `EmailVerificationService`, smtp4dev accepted the synthetic test message, and the inbox API listed its fixed test recipient. No code/body or credentials were retained. TC04 GUI entry remains unverified; E2 remains PARTIAL because outage recovery and production SMTP are not tested. | Isolated Release build and `DanToPhong.Protocol.Smoke.dll --smtp4dev-otp-queue`; local `GET /api/messages`; [E2-smtp4dev-queue-delivery.log](evidence/E2-smtp4dev-queue-delivery.log) |
| Avoid the active server output lock | The normal `dotnet run` attempt could not replace a Protocol DLL held by the already-running server PID 13472. Rebuilt and ran from project-local ignored `bin\\verify-email-queue` outputs without stopping the server. | `dotnet build ... '-p:BaseOutputPath=bin\\verify-email-queue\\'`; isolated smoke DLL |

## 2026-11-27 - Removing synchronous heartbeat wait during client disposal

| Work | Result | Commands / checks |
|---|---|---|
| Make `NetworkClientService.Dispose` nonblocking | Dispose now cancels heartbeat and closes the socket without synchronously waiting. New requests are rejected; shared semaphore/cancellation resources are released asynchronously after tracked requests and heartbeat finish. | Source review of `SendAndReceiveAsync`, heartbeat timer, and client call sites |
| Build Launcher | Release build passed with 0 errors and 6 existing nullable-context warnings in generated Designer files. Isolated output avoided the running server's locked Protocol DLL. | `dotnet build .\\UI\\UI.csproj -c Release --no-restore '-p:BaseOutputPath=bin\\verify-network-dispose\\'` |

## 2026-11-27 - Running T2-T8 over real TCP protocol clients

| Work | Result | Commands / checks |
|---|---|---|
| Add reproducible protocol harness | Added `tools/T2T8ProtocolTest` and a PowerShell runner. It starts isolated Server processes on loopback with explicitly enabled plaintext, creates random test accounts only in `DanToPhongDB_Test`, and removes them in cleanup. No password, token, or connection string is written to evidence. | `tools\\run-t2t8-protocol-test.ps1`; isolated build output `bin\\t2t8-protocol` |
| Execute T2-T8 protocol checks | All T2-T8 assertions passed: two authenticated clients/session logs; PONG 3.3/0.9 ms; chat send/poll 6.3 ms; 20 exact ordered messages; Vietnamese round-trip; abrupt client reset with server session removal and peer event/snapshot; test server termination detected on both clients' command and stream sockets. | [T2-T8-protocol-console-20261127-173442.log](evidence/T2-T8-protocol-console-20261127-173442.log) |
| Record evidence scope | Aggregate report status is PARTIAL because these are console/protocol checks only; Launcher/Unity GUI rendering and close-button interaction still require manual testing. This run does not replace TC05 TLS evidence. | Updated Chapter 3 report, requirements traceability, evidence index, and historical T1-T8 log |

## 2026-11-27 - Aligning README gameplay limits with the supplied version

| Work | Result | Commands / checks |
|---|---|---|
| Update README gameplay limitations | Replaced the differing paragraph with the supplied wording; the remainder already matched. | `git diff --check`; review `git diff -- README.md` |

## 2026-11-27 - Enforcing pinned TLS and an optional secondary listener

| Work | Result | Commands / checks |
|---|---|---|
| Add shared SHA-256 certificate pinning | Launcher requires `certificateThumbprintSha256`; Launcher passes the public pin to Unity via child-process environment; shared validation preserves host/time checks and permits only a pinned self-signed `UntrustedRoot`. | Server and Launcher Release builds; protocol smoke tests |
| Add optional secondary listener | `DANTOPHONG_SECONDARY_PORT` is disabled by default; configured port shares the same TLS certificate, protocol and concurrency controls. | TC05 listener run on 19001; default server runs on 19000 |
| Rerun TC05 | Four transport cases PASS against `DanToPhongDB_Test` on loopback 19000; wrong pin and plaintext rejected; explicit plaintext succeeds. PFX was encrypted with a process-only generated password, stored outside repo, and not imported into a certificate store. | `tools\run-tc05-pinned.ps1`; [TC05-pinned-local-20261127-180428.log](evidence/TC05-pinned-local-20261127-180428.log) |
| Update security/config documentation | Updated README, protocol/security notes, traceability, evidence index, and client/server config examples; no AWS command or GUI test. | `git diff --check` |

## 2026-11-27 - Rerunning T1-T8 on a dedicated protocol-test port

| Work | Result | Commands / checks |
|---|---|---|
| Rerun T1-T8 console protocol checks | T1 listener and T2-T8 behavior observed on loopback port 19000; both temporary accounts were removed. PASS applies to console/protocol only; Launcher/Unity GUI remains manual. | `tools\run-t2t8-protocol-test.ps1`; [T2-T8-protocol-console-20261127-180529.log](evidence/T2-T8-protocol-console-20261127-180529.log) |
| Verify test server cleanup | Harness exited its server processes; `Get-NetTCPConnection -State Listen -LocalPort 19000` returned no listeners. Existing port 9000 PID 13472 was not stopped. | Port check after the harness |

## 2026-11-27 - Completing local-run guidance and fixing room-list launch

| Work | Result | Commands / checks |
|---|---|---|
| Make room-list join launch Unity | The dynamic room-card action previously showed only a notice; it now launches the game with the selected room code so the existing lobby flow can join or spectate. GUI behavior remains unverified. | `dotnet build .\UI\UI\UI.csproj -c Release --no-restore` |
| Add local two-client and Internet demo instructions | README documents test TLS pin setup and two Launcher instances; the demo runbook documents the approved manual AWS sequence only. No AWS command or resource action was used. | Review README and `docs/demo-runbook.md` |
| Protect local runner environment | `run-local.ps1` restores the process-scoped test DB password as well as other temporary variables; PowerShell syntax parse passed. | PowerShell AST parse of `scripts/run-local.ps1` |
| Validate source changes and core smoke | Server Release build passed with 0 warnings; Launcher Release build passed with 6 pre-existing generated Designer nullable warnings; protocol smoke passed, including forged `UserId`/`TargetId` elimination isolation and certificate pin validation. | `dotnet build` Server/Launcher Release; `dotnet run` Protocol.Smoke |
| Rebuild Unity with pinned TLS client code | Unity 6000.3.28f1 batch build reported Build Successful, exited 0, and had no compiler/build failure markers; this is packaging evidence, not gameplay or connection evidence. | `BuildWindowsPlayer.BuildWindows64Release`; [TC08 log](evidence/TC08-unity-build.log) |

## 2026-11-27 - Verifying the local TLS run script

| Work | Result | Commands / checks |
|---|---|---|
| Run `scripts/run-local.ps1` against local test services | Release build passed with 0 warnings; only `dtp-sql`/`dtp-smtp` were inspected; `DanToPhongDB_Test` initialized and connected; TLS listener opened on 0.0.0.0:19000 and printed client endpoint/pin without printing DB credentials. | `.\scripts\run-local.ps1`; inspect PID owning 19000 |
| Stop the test Server | Stopped only the verified Server PID 45496 after confirming its command line; port 19000 was clear afterward. The command ended with -1 because the process was deliberately terminated; the build/startup had already succeeded. Two encrypted PFX test files remain outside the repository. | `Stop-Process -Id 45496`; `Get-NetTCPConnection -State Listen -LocalPort 19000` |

## 2026-11-27 - Restricting local TLS test binding

| Work | Result | Commands / checks |
|---|---|---|
| Bind the local runner to loopback | The first run exposed that the general TLS default binds all interfaces. Added optional `DANTOPHONG_LISTEN_ADDRESS`, made plaintext reject non-loopback binds, and set the local runner to `127.0.0.1`. | Server source and local runner update |
| Rerun and stop the local Server | Build and test DB startup passed; log reported `127.0.0.1:19000` with TLS enabled. Stopped only verified PID 38020; `19000` was clear. | `.\scripts\run-local.ps1`; `Stop-Process -Id 38020`; port check |

## 2026-11-27 - Rechecking T1-T8 after bind hardening

| Work | Result | Commands / checks |
|---|---|---|
| Rerun protocol T1-T8 | Dedicated loopback listener and both authenticated TCP clients observed; PONG 3.5/1.1 ms, chat 11.2 ms, ordered 20-message and Vietnamese round-trips, abrupt disconnect event, and server-shutdown detection passed. Temporary accounts removed. GUI remains manual. | `tools/run-t2t8-protocol-test.ps1`; [latest log](evidence/T2-T8-protocol-console-20261127-182612.log) |
| Verify cleanup | Harness exited its server processes; port 19000 returned clear. Port 9000 pre-existing listener was not stopped. | `Get-NetTCPConnection -State Listen -LocalPort 19000` |

## 2026-11-27 - Forcing local-run email to smtp4dev

| Work | Result | Commands / checks |
|---|---|---|
| Prevent real SMTP fallback in local runner | The runner now sets `DANTOPHONG_SMTP_MODE=smtp4dev`, loopback host/port, dummy sender, and blank process-scoped credentials; it refuses startup if `dtp-smtp` port 2525 is unreachable. The previous runner startup sent no email. | Source review; PowerShell AST parse |
| Verify local dependencies | Loopback ports 2525 and 1433 were reachable; no container was started or stopped and no SMTP message was sent. | `Test-NetConnection 127.0.0.1 -Port 2525`; `Test-NetConnection 127.0.0.1 -Port 1433` |

## 2026-11-27 - Preparing handoff documentation

| Work | Result | Commands / checks |
|---|---|---|
| Write overnight report and morning checklist | Recorded current R/G/T/E and TC01-TC12/T1-T8 statuses, exact gameplay validation boundaries, local commit hashes, manual-only work, and safe reproduction steps. No unsupported PASS claims or personal data added. | Review evidence links and `git diff --check` |

## 2026-11-27 - Tightening secret/build ignore checks

| Work | Result | Commands / checks |
|---|---|---|
| Ignore local secret and certificate variants | Added common `.env`, private-key, certificate, and keystore suffixes without hiding the checked-in `.example` configuration files. | `git check-ignore --no-index` on synthetic paths |
| Audit targeted C# source patterns | No `.Result`, blocking `.Wait()`, `DoEvents`, `Thread.Sleep`, BeginReceive, or EndReceive matches in Server/Protocol/Launcher source. SQL call sites were located, but a full query-by-query parameter audit was not repeated in this pass; prior R4 evidence remains PARTIAL. | `rg` searches in `UI/DanToPhong.Server`, `UI/DanToPhong.Protocol`, and `UI/UI` |
| Verify architecture/ignore hygiene | Legacy networking-library search returned no matches; build/zip/secret ignore probes passed. No AWS commands or changes. | source search; `git check-ignore -v --no-index` |
