# Networking-course requirements checklist

Status distinguishes source-level implementation from observed runtime
verification. A successful build or framing smoke test is not a multiplayer
runtime pass.
The canonical consolidated report is
[BAO_CAO_DO_AN_NHAP_MON_MANG.md](../BAO_CAO_DO_AN_NHAP_MON_MANG.md).

| Requirement | Status | Evidence / remaining work |
|---|---|---|
| C# client with GUI | PARTIAL | WinForms UI under `UI/UI/`; Unity C# game has its own GUI. Needs current-build interactive verification. |
| At least two GUI clients at once | PARTIAL | Two Unity instances were previously observed at the main menu, but not logged into one live match. The latest two-client room test used protocol clients, not GUI. |
| Server implemented in an allowed language with database | SOURCE IMPLEMENTED; LOCAL TEST DB USED | C# server and SQL repositories in `UI/DanToPhong.Server/`; TC05 used the isolated `DanToPhongDB_Test`. Authenticated application workflows remain unverified. |
| Team-developed client/server network stack | PARTIAL | Custom TCP/TLS framing plus authenticated UDP movement input; four concurrent racers were verified protocol-level ([latest log](evidence/local-protocol-integration-20261127-213927.log)). GUI/Internet gameplay remains unverified. |
| Client/server ports and message structures specified | DOCUMENTED | `docs/protocol.md`: TCP/TLS 9000 with big-endian length + UTF-8 JSON, UDP 9000 fixed 74-byte HMAC movement datagram, commands, DTOs and limits. |
| Multiple concurrent connections | PARTIAL | TC10 verified 50 anonymous loopback clients, p95 1.7 ms/max 5.2 ms with a slow incomplete-header sender, malformed/oversized rejection; four UDP racer inputs were separately verified. Authenticated sustained load and impairment remain untested ([latest log](evidence/local-protocol-integration-20261127-213927.log)). |
| Internet operation | NOT VERIFIED | Pinned self-signed TLS passed only on loopback ([TC05](evidence/TC05-pinned-local-20261127-184719.log)); public endpoint/SAN, ingress, database connectivity and two independent client networks require approved manual validation. |
| Account registration/login/OTP over custom protocol | PARTIAL; E2E NOT VERIFIED | The local DB harness rejected wrong/expired OTPs, enforced single use and lockout after five attempts, and exercised the login limiter. Existing SMTP receipt is separate evidence; new email delivery and code entry through Launcher remain unverified ([integration log](evidence/local-protocol-integration-20261127-202549.log)). |
| Lobby/room membership over custom protocol | PARTIAL | Four authenticated racers exercised create/join/start; an additional client spectated and received snapshots. Reconnect works within grace period; in-memory state does not survive process restart. GUI remains unverified ([latest integration log](evidence/local-protocol-integration-20261127-213927.log)). |
| Gameplay state over custom protocol | PARTIAL / NOT READY | Four racers sent signed sequenced UDP input; server snapshot positions and events remain on TCP/TLS. Finish/winner authority, full physics, server-owned timer/elimination, and live GUI match remain unverified. |
| Solo mode | SOURCE PRESENT; RUNTIME NOT VERIFIED | Local Unity vehicle flow exists; build/scene and real playthrough must be rechecked after current edits. |
| Git-managed source and reproducible build | PARTIAL | Unity-generated solution, Server and Launcher Release builds passed with 0 errors (7 Unity warnings, 0 Server warnings, 6 generated-Designer Launcher warnings) ([latest log](evidence/local-release-build-20261127-2030.log)). A final clean-clone check remains. |
| Architecture, GUI, application flow, three user stories | DOCUMENTED; PNG EXPORTED | Four Draw.io PNGs were exported and visually reviewed; conceptual GUI wireframe is not a screenshot of the running app. |
| Assignment roles/contact information | NEEDS OWNER INPUT | Report placeholders must be completed from actual group records; no personal details should be invented. |

## Actual network paths

| Feature | Current path | Status |
|---|---|---|
| Registration, login, OTP, password reset, profile, inventory and reviews | Custom TCP protocol, port 9000 | Source exists; runtime configuration and end-to-end tests required. |
| Room create/join/leave/state/start | Custom TCP protocol, port 9000 | In-memory server-side membership, spectate and reconnect during a 120-second grace period; no restart recovery. |
| Vehicle movement input | Custom UDP protocol, port 9000; binding via TCP/TLS 9000 | Target 20 Hz, fixed 74-byte HMAC-SHA-256 datagrams with increasing sequence; four racers verified by loopback harness. |
| Player snapshots and important events | Custom TCP/TLS protocol, port 9000 | Authenticated stream, ordered event frames and nominal 20 Hz snapshots; server simulation remains simplified. |
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

UDP broadcast discovery is still a future possibility. The UDP movement input
channel is implemented and loopback-tested; GUI, Internet and impairment tests
remain outside current claims.
