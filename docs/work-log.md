# Work log

## 2026-11-27 — TCP-only cleanup and verification

| Work | Result | Commands / checks |
|---|---|---|
| Remove third-party networking assets and serialized references | Removed `Assets/Photon`, its meta file, four scene components, the scooter prefab component, scripting defines, and stale ignored generated project files. `git grep` and scene-ID scans found no remaining references. | `git grep -ni -e photon -e pun_2`; Unity asset GUID/fileID scans |
| Align documentation/configuration | Updated protocol, architecture, requirements checklist, report, README, evidence status, diagrams and client example. Recorded gameplay as incomplete where server broadcast/track authority/reconnect are absent. | Draw.io XML parse; client example JSON parse |
| Fix Unity compilation issues and unused state | Qualified `UnityEngine.Random`; removed write-only game-result flags and obsolete host-client comment. | Unity Editor build |
| Restrict local test harnesses | TCP load/malformed-frame and login-lockout scripts reject non-loopback hosts before connecting; both guard tests passed without network access. | Script inspection; loopback guard tests |
| Verify server, Launcher and framing | Server Release build passed with 0 warnings/errors. Launcher publish passed with 6 existing generated-Designer `CS8669` warnings. Protocol framing smoke test passed. | `dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore`; `dotnet restore .\UI\UI\UI.csproj -r win-x64`; `dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false --no-restore -o D:\DanToPhong\LauncherRelease`; `dotnet run --project .\UI\DanToPhong.Protocol.Smoke\DanToPhong.Protocol.Smoke.csproj -c Release` |
| Verify Unity Windows x64 | Build succeeded; no C# compiler diagnostics in final run. Unity emitted non-fatal licensing/package HTTPS messages. Player was not launched. | Unity 6000.3.28f1 `-batchmode -nographics -quit -executeMethod BuildWindowsPlayer.BuildWindows64Release` |
| Safety and Git checks | Secret-pattern scan found no credential values; staged changes are limited to the TCP-only cleanup, related code/docs, and removal of third-party networking assets. No AWS commands or Git push were run. | `git diff --check`; `git diff --cached --check`; `git grep`; credential-pattern scan; `git status --short` |

Two-client gameplay, TLS runtime cases, SMTP/DB runtime, load, DPI, packet capture,
and Internet tests remain unverified; see `docs/evidence/README.md`.
