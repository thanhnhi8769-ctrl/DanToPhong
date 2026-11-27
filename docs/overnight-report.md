# Overnight report

Updated: 2026-11-27. Branch: `feature/own-netcode`. No AWS command or resource
action was performed; nothing was pushed.

## R/G/T/E summary

| Area | Status | Result |
|---|---|---|
| R — requirements and network behavior | PARTIAL | Custom TCP framing and TLS pinning are implemented. The room-list join action was wired to launch the selected room. Gameplay checks and unverified limits are listed below; a live two-client match is still missing. |
| G — GUI | BLOCKED / MANUAL | No interactive Launcher/Unity match or OTP entry was observed. Source/build success is not GUI evidence. |
| T — tests | PARTIAL | TC05 passed 4/4 on loopback; console T1–T8 passed at protocol level; Server, Launcher, Unity, and protocol smoke builds/tests passed with the scopes in the table below. |
| E — evidence | PARTIAL | Logs are indexed under [docs/evidence](evidence/README.md). Live-match, OTP-entry, SMTP-outage, and independent-network evidence is absent. |

## TC01–TC12 and T1–T8

| Test | Status | Observed scope / evidence |
|---|---|---|
| TC01 | PASS — limited | Local TCP reachability only; not a two-network application test. [Evidence](evidence/TC01-tcp-reachability.log) |
| TC02 | BLOCKED | No two clients on independent networks. [Evidence note](evidence/TC02-two-client-gui.log) |
| TC03 | BLOCKED | No live two-client GUI room/game observation. |
| TC04 | PARTIAL | SMTP registration mail receipt exists; correct/wrong/expired OTP entry and attempt lockout were not observed. [Evidence](evidence/TC04-prior-otp-delivery.log) |
| TC05 | PASS — loopback | Pinned TLS+TLS, rejection of TLS/plaintext and wrong certificate, and explicit plaintext case: 4/4; optional TLS listener also observed. [Latest pinned run](evidence/TC05-pinned-local-20261127-180428.log) |
| TC06 | PASS — prior local capture | Plaintext/TLS payload comparison; does not establish Internet confidentiality. [Evidence](evidence/TC06-TC11-TC12-loopback-capture.log) |
| TC07 | BLOCKED | No live match reconnect/server-restart observation. |
| TC08 | PASS — build only | Unity 6000.3.28f1 Windows x64 player build succeeded after the pinning changes; no player launch or server connection was tested. [Evidence](evidence/TC08-unity-build.log) |
| TC09 | PASS — in-memory smoke | Framing, event/snapshot queues, account bounds, OTP queue retry simulation, pin checks, and forged `UserId`/`TargetId` elimination isolation. Not a live gameplay test. [Evidence](evidence/TC09-framing-smoke.log) |
| TC10 | PARTIAL | 50 anonymous loopback PING clients plus malformed/oversized/framing probes; no authenticated gameplay load or Clumsy impairment. [Evidence](evidence/TC10-loopback-framing-test.log) |
| TC11 | PASS — prior local capture | Npcap loopback SYN/SYN-ACK and FIN/FIN-ACK. [Capture evidence](evidence/TC06-TC11-TC12-loopback-capture.log) |
| TC12 | PASS — prior local capture | TLS handshake/cipher observed on Npcap loopback; not an Internet test. [Capture evidence](evidence/TC06-TC11-TC12-loopback-capture.log) |
| T1–T8 | PARTIAL | Console harness observed loopback listener, two authenticated clients, PONG/chat below one second, 20 ordered messages, Vietnamese round-trip, abrupt peer cleanup, and both clients detecting server shutdown. GUI remains manual. [Latest timestamped log](evidence/T2-T8-protocol-console-20261127-182612.log) |

## Gameplay boundary and known limitations

A client cannot use `ELIMINATE` with a forged `UserId` or `TargetId` to remove
another racer, declare that racer the winner, or trigger a winner reward. The
smoke case observed that only the caller's own racer is affected. A client can
still request its own elimination as a forfeit.

The Server currently checks that gameplay input belongs to the authenticated
player in a running, nonterminal match; `Throttle` and `Steer` are finite and
within `[-1, 1]`; and `ClientTick` strictly increases. `ClientTimestampMs` is
required but its freshness/skew is not checked. The client does not submit an
accepted position: the Server simulates motion at 50 ms steps, clamps speed to
`[-5, 25]`, brakes after 0.5 seconds without input, and performs at most four
catch-up steps. Checkpoints advance only through the next configured gate;
finish is accepted only after the required checkpoints and finish-gate crossing.

There is no full Unity collision/vehicle physics on the Server, no authoritative
fall/off-track elimination, no race timer or completion deadline, no reverse
direction check, and no separate abnormal-teleport detector. The simplified
simulation is not a substitute for those checks. Spectate and a 120-second
reconnect grace period exist in source but were not verified in a live match.
Room/game state is process-local and is lost on Server restart. Do not describe
this as complete server-authoritative gameplay or release-ready multiplayer.

## Local changes and test observations

- `5564c7c` — pinned TLS, fail-closed tests, optional listener, and local
  setup/docs.
- `cd4a7b3` — ignore rules for local secrets, certificates, archives, and
  build outputs.
- `1f90716` — configurable bind address; local TLS runner binds loopback.
- `828b8f6` — latest T1–T8 evidence and smtp4dev-only local-run safeguards.
- Server Release build: pass, 0 warnings. Launcher Release build: pass, 6
  existing generated Designer nullable-context warnings. Protocol smoke: pass.
- Unity Windows x64 batch build: pass; no player launch. Local run script built
  Server with 0 warnings, connected only to `DanToPhongDB_Test`, and opened
  TLS on `127.0.0.1:19000`; the exact test process was stopped and 19000 cleared.
- Legacy networking-library scan: no tracked matches. Ignore probes passed for
  env, server config, keys/certificates, archives, and build output while
  keeping `.example` files trackable.
- Four feature/test commits above are local. No push.

## Blocked/manual items and risks

See [docs/blocked.md](blocked.md) and the [morning checklist](morning-checklist.md).
The pre-existing listener on port 9000 (PID 13472) was not stopped. Ports 19000
and 19001 were clear at the last test-process check. Several self-signed PFX
test artifacts remain outside the repository; one older TC05 artifact used an
empty export password. None was imported into a certificate store or written
into Git; verify and remove only exact known test files manually.

No AWS actions were attempted. Internet reachability, current Free Tier/costs,
real trusted-hostname certificate deployment, GUI OTP entry, SMTP outage
recovery, authenticated load, and two-client gameplay remain unverified.
