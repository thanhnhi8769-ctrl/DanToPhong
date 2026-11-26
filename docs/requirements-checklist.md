# Networking-course requirements checklist

Status distinguishes source-level implementation from observed runtime
verification. A successful build or framing smoke test is not a multiplayer
runtime pass.

| Requirement | Status | Evidence / remaining work |
|---|---|---|
| C# client with GUI | PARTIAL | WinForms UI under `UI/UI/`; Unity C# game has its own GUI. Needs current-build interactive verification. |
| At least two GUI clients at once | PARTIAL | Launcher and Unity are separate clients; two Unity instances are also intended. No two-client runtime evidence is currently recorded. |
| Server implemented in an allowed language with database | SOURCE IMPLEMENTED; RUNTIME NOT VERIFIED | C# server and SQL repositories in `UI/DanToPhong.Server/`; local/cloud DB connection and concurrent run need verification. |
| Team-developed client/server network stack | PARTIAL | TCP framing and protocol are in `DanToPhong.Protocol`; no third-party networking library is used. Multiplayer path remains an incomplete request/response prototype. |
| Client/server ports and message structures specified | DOCUMENTED | `docs/protocol.md`: TCP 9000, 4-byte big-endian length prefix, UTF-8 JSON, command names, DTO fields and limits. |
| Multiple concurrent connections | SOURCE IMPLEMENTED; LOAD NOT VERIFIED | Async accept/session handling and a concurrent session limit are in `UI/DanToPhong.Server/Program.cs`; TC10/load evidence is absent. |
| Internet operation | NOT VERIFIED | Runtime TLS certificate, endpoint, security-group ingress, database and two independent client networks require manual validation. |
| Account registration/login/OTP over custom protocol | SOURCE IMPLEMENTED; E2E NOT VERIFIED | Commands and server handlers exist; mail/DB runtime, valid/invalid/expired OTP and limiter tests still need evidence. |
| Lobby/room membership over custom protocol | PARTIAL | `ROOM_CREATE`, `ROOM_JOIN`, `ROOM_LEAVE`, `ROOM_STATE`, `ROOM_START` use in-memory server registry. No persistence/resume and no independent push to all clients. |
| Gameplay state over custom protocol | PARTIAL / NOT READY | `GAME_INPUT` and `GAME_SNAPSHOT` exist. Server returns a snapshot per request, uses simplified kinematics, and does not model track collision/validate physical checkpoint crossing. No async broadcast, bounded server push queue, spectator flow or reconnect resume. |
| Solo mode | SOURCE PRESENT; RUNTIME NOT VERIFIED | Local Unity vehicle flow exists; build/scene and real playthrough must be rechecked after current edits. |
| Git-managed source and reproducible build | PARTIAL | Git repository and build instructions exist. Current worktree is dirty; final clean clone/build and source scan remain outstanding. |
| Architecture, GUI, application flow, three user stories | DOCUMENTED; IMAGES NEED REVIEW | Draw.io XML files exist under `docs/diagrams/`; exported PNGs and alignment with latest source have not been verified. |
| Assignment roles/contact information | NEEDS OWNER INPUT | Report placeholders must be completed from actual group records; no personal details should be invented. |

## Actual network paths

| Feature | Current path | Status |
|---|---|---|
| Registration, login, OTP, password reset, profile, inventory and reviews | Custom TCP protocol, port 9000 | Source exists; runtime configuration and end-to-end tests required. |
| Room create/join/leave/state/start | Custom TCP protocol, port 9000 | In-memory server-side membership; no durable reconnect. |
| Vehicle input and player snapshots | Custom TCP protocol, port 9000 | Request/response prototype; server simulation is simplified and no room-wide push exists. |
| Chat | Custom TCP protocol, port 9000 | In-memory send/poll; not pushed asynchronously. |
| Solo vehicle simulation | Local Unity physics | Does not require the server for movement. |
| Rewards, verified checkpoint crossing, spectating and match resume | Not fully implemented | Do not claim server authority or mark corresponding tests PASS. |

## Minimum completion plan

1. Preserve the existing framed TCP/TLS transport and request/response account
   protocol.
2. Complete room lifecycle and a serialized, bounded per-client server push
   mechanism before claiming real-time broadcast.
3. Replace prototype movement with track-aware authoritative validation,
   server-verified checkpoints/results, and explicitly defined disconnect /
   rejoin semantics.
4. Add protocol tests for malformed/slow clients and concurrent load, then
   observe two GUI clients in a real match.
5. Record only observed results and sanitized evidence in `docs/evidence/`.

Any future datagram transport is a design possibility only and is outside the
current implementation and test claims.
