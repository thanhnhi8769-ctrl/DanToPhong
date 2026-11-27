# Screenshot checklist for partial and blocked tests

This checklist is a capture plan, not test evidence. Do not mark a test PASS
from a screenshot alone. Existing console logs prove only their stated
protocol-level scope; GUI behavior still needs to be observed directly.

Save only sanitized screenshots in this directory. Capture real application,
server, SMTP4dev, or test-tool windows; do not compose multiple captures into a
misleading result. Record the test ID, local time/time zone, build or commit,
and outcome in the evidence index. Redact before saving any screenshot that
shows personal email/usernames, public IPs, OTPs, passwords, tokens,
connection strings, certificate/private-key material, or unrelated desktop
content. Use disposable test accounts and data.

For Internet cases, do not start AWS resources or change security groups
without separate owner approval. If approved, allow only the verified tester
address as `/32` on TCP 9000; never open `0.0.0.0/0` or SQL port 1433.

| Test | Screenshot filename(s) | Actions to perform | Must be visible | Redact before saving |
|---|---|---|---|---|
| TC02 — BLOCKED | `tc02-client-a-cross-network-room.png`; `tc02-client-b-cross-network-room.png` | After the approved Internet server is ready, use two clients on independent networks and separate test accounts. Sign in, create/join the same room, and observe a live state update. Capture each machine separately if both cannot fit on one screen. | Each client connected to the same test endpoint/room; room membership or synchronized state; optionally a separate server log showing both sessions. | Always mask real public IPs, account email/name, tokens, passwords, room invite codes, connection strings, unrelated server log lines. |
| TC03 — BLOCKED | `tc03-room-create-join.png`; `tc03-snapshot-both-clients.png` | On two GUI clients, create and join a TCP room, start the match, then move one racer and verify the other client receives an updated snapshot. | Create/join result; both Unity windows in the same room; a visible state change on both clients and matching server-side event/snapshot log if available. | User identifiers, tokens, public endpoint, any personal account details. Do not infer a snapshot from two static menus. |
| TC04 — PARTIAL | `tc04-otp-smtp4dev-mail.png`; `tc04-otp-valid-success.png`; `tc04-otp-wrong-rejected.png`; `tc04-otp-expired-rejected.png`; `tc04-otp-attempt-lockout.png` | Use a disposable registration/reset account. Show delivery in SMTP4dev (`http://localhost:5000`), enter the valid code, then separately test wrong, expired, and repeated-invalid attempts. Use a fresh code/account for each independent flow where needed. | SMTP4dev shows the test message and timestamp; Launcher shows the corresponding accepted/rejected/expired/locked outcome. Match email delivery to the app outcome without exposing the code. | Always mask the full OTP in SMTP4dev and Launcher, recipient/sender if personal, password fields, account email/name, session token, and DB/SMTP configuration. |
| TC07 — BLOCKED | `tc07-reconnect-within-grace.png`; `tc07-server-restart-outcome.png` | During a real match, disconnect one client and reconnect within the documented 120-second grace period. In a separate run, restart the test server and record what both clients actually report and whether room state is lost. | Reconnect attempt and resulting room/racer state; for restart, visible connection-loss message and observed post-restart state. Include relevant server log timestamps without secrets. | Tokens, account identifiers, public IP, connection strings, private certificate data. Do not describe a reconnect as successful if the client returns only to a menu. |
| TC10 — PARTIAL | `tc10-load-summary.png`; `tc10-slow-malformed-disconnect.png` | Run the approved loopback harness at the selected concurrency (record whether 10 or 50), then perform slow-send, abrupt disconnect, malformed, and oversized-frame cases. Do not use Clumsy if it requires elevated/system changes. | Harness summary with time, attempted/connected/completed/failed counts and measured latency fields actually produced; server log showing handling/cleanup of slow or invalid clients. | Any account data, payload content, host IP, DB connection details, secrets, or raw stack traces containing sensitive values. |
| T2–T3 — PARTIAL (GUI) | `t2-t3-two-clients-connected.png` | Launch two GUI clients against the local TLS-pinned server on port 19000, sign in with separate disposable accounts, and wait for both sessions to connect. The existing console harness remains protocol-level evidence. | Two client windows and server log showing two accepted/authenticated sessions with timestamps; distinguish connection success from room/gameplay success. | Emails/usernames as needed, tokens, certificate password, DB string, unrelated process output. |
| T4 — PARTIAL (GUI) | `t4-message-delivery-time.png` | If the GUI has a message/chat action, send one unique test message and capture sender/receiver timestamps. If no GUI action exists, retain the console log as protocol evidence and do not claim GUI delivery. | Same unique message at sender and receiver plus a measured send-to-receive duration under one second. | Personal text, identity, tokens, server endpoint, other chat content. |
| T5–T6 — PARTIAL (GUI) | `t5-t6-sequence-and-vietnamese.png` | If the GUI exposes chat/test messages, send 20 numbered messages consecutively and one Vietnamese string, then verify every ID/content at the receiver. Otherwise retain the console harness evidence and record GUI as unverified. | All 20 messages in order with count/IDs and the Vietnamese text rendered correctly at the receiver. | Account identity, private messages, tokens, endpoint, unrelated desktop content. |
| T7 — PARTIAL (GUI) | `t7-peer-close-and-notification.png` | In a real two-client room, close client A using its window close button; observe server cleanup and client B's disconnect event/state. | Client A closed; server session removal; client B's peer-disconnected message or state update with matching timestamps. | User identities, tokens, public endpoint and private logs. |
| T8 — PARTIAL (GUI) | `t8-server-stop-and-client-loss.png` | With two clients connected, stop only the test server started for this run using its own terminal; observe both clients. | Server stop in its terminal and both clients showing connection loss/timeout; no claim of graceful game recovery unless observed. | Credentials, tokens, connection strings, unrelated terminal history. |

TC01, TC05, TC06, TC08, TC09, TC11, and TC12 currently have indexed log or
capture evidence. A Wireshark screenshot such as
`tc06-11-12-wireshark-loopback.png` is optional corroboration: show the
Npcap Loopback interface, relevant TCP port/flags or TLS handshake, and keep
the packet payload and non-loopback addresses out of the image. Do not replace
the original sanitized pcap/log with a screenshot.

