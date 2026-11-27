# Networking-course requirements checklist

Status distinguishes source-level implementation from observed runtime
verification. A successful build or framing smoke test is not a multiplayer
runtime pass.
The canonical consolidated report is
[BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md](../BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md).

| Requirement | Status | Evidence / remaining work |
|---|---|---|
| C# client with GUI | PARTIAL | WinForms UI under `UI/UI/`; Unity C# game has its own GUI. Needs current-build interactive verification. |
| At least two GUI clients at once | PARTIAL | Two Unity instances opened to the main menu simultaneously; a live login/match between them remains unverified. |
| Server implemented in an allowed language with database | SOURCE IMPLEMENTED; LOCAL TEST DB USED | C# server and SQL repositories in `UI/DanToPhong.Server/`; TC05 used the isolated `DanToPhongDB_Test`. Authenticated application workflows remain unverified. |
| Team-developed client/server network stack | PARTIAL | TCP framing and protocol are in `DanToPhong.Protocol`; the 2026-11-27 console run verifies T2–T8 only at protocol level ([log](evidence/T2-T8-protocol-console-20261127-184734.log)). Gameplay GUI remains unverified. |
| Client/server ports and message structures specified | DOCUMENTED | `docs/protocol.md`: TCP 9000, 4-byte big-endian length prefix, UTF-8 JSON, command names, DTO fields and limits. |
| Multiple concurrent connections | PARTIAL | Async accept/session handling and a concurrent session limit are implemented; TC10 verified 50 anonymous loopback PING clients plus framing edge cases, but authenticated/gameplay sustained load and impairment remain untested. |
| Internet operation | NOT VERIFIED | Pinned self-signed TLS passed only on loopback ([TC05](evidence/TC05-pinned-local-20261127-184719.log)); public endpoint/SAN, ingress, database connectivity and two independent client networks require approved manual validation. |
| Account registration/login/OTP over custom protocol | PARTIAL; E2E NOT VERIFIED | smtp4dev accepted a synthetic SMTP sample in the latest local smoke run ([log](evidence/local-build-smoke-20261127-184655.log)); registration-mail receipt exists, but OTP entry/expiry/attempt lockout and login limiter still need runtime evidence. |
| Lobby/room membership over custom protocol | PARTIAL | `ROOM_CREATE`, `ROOM_JOIN`, `ROOM_LEAVE`, `ROOM_STATE`, `ROOM_START` use in-memory server registry. Subscribed clients receive ordered event/snapshot frames; spectate/reconnect paths retain state for a 120-second grace period, but no state survives server restart. |
| Gameplay state over custom protocol | PARTIAL / NOT READY | `GAME_INPUT` requests and `GAME_STREAM` push ordered events/snapshots over TCP; server uses simplified kinematics and configured checkpoint/finish gates; Unity bounds its snapshot queue at eight and interpolates remote state. Full physics, server-owned timer/elimination, and live two-client evidence are still absent. |
| Solo mode | SOURCE PRESENT; RUNTIME NOT VERIFIED | Local Unity vehicle flow exists; build/scene and real playthrough must be rechecked after current edits. |
| Git-managed source and reproducible build | PARTIAL | Server/Launcher/Unity and clean-clone builds passed at an earlier commit; the current documentation-only changes still need final clean-clone verification and source scan. |
| Architecture, GUI, application flow, three user stories | DOCUMENTED; PNG EXPORTED | Four Draw.io PNGs were exported and visually reviewed; conceptual GUI wireframe is not a screenshot of the running app. |
| Assignment roles/contact information | NEEDS OWNER INPUT | Report placeholders must be completed from actual group records; no personal details should be invented. |

## Actual network paths

| Feature | Current path | Status |
|---|---|---|
| Registration, login, OTP, password reset, profile, inventory and reviews | Custom TCP protocol, port 9000 | Source exists; runtime configuration and end-to-end tests required. |
| Room create/join/leave/state/start | Custom TCP protocol, port 9000 | In-memory server-side membership, spectate and reconnect during a 120-second grace period; no restart recovery. |
| Vehicle input and player snapshots | Custom TCP protocol, port 9000 | 15–20 Hz input requests plus an authenticated server-push stream for ordered events and nominal 20 Hz snapshots; server simulation remains simplified. |
| Chat | Custom TCP protocol, port 9000 | In-memory send/poll; not pushed asynchronously. |
| Solo vehicle simulation | Local Unity physics | Does not require the server for movement. |
| Checkpoint/finish gates, winner reward, spectating and reconnect | SOURCE PRESENT; LIVE TEST NOT VERIFIED | Gate checks and winner reward are server-side; spectate/reconnect are in-memory for a 120-second grace period. Timer/elimination authority and live-match evidence remain incomplete. |

## Minimum completion plan

1. Preserve the existing framed TCP/TLS transport and request/response account
   protocol.
2. Bounded per-client event/snapshot mailboxes are implemented: important
   events wait for capacity and stale snapshots are dropped. Add live
   integration tests for the serialized per-client stream.
3. Close the remaining authority gap with a server-owned match timer and
   elimination decision; keep the simplified simulation clearly bounded and
   verify configured gates and disconnect/rejoin behavior in a live match.
4. Extend the bounded TC10 checks to authenticated/gameplay load and impairment,
   then observe two GUI clients in a real match.
5. Record only observed results and sanitized evidence in `docs/evidence/`.

Any future datagram transport is a design possibility only and is outside the
current implementation and test claims.
