# Blocked and manual verification items

Only items not demonstrated in this work session are listed here. Reclassify
them only after observing the result and saving sanitized evidence.

| Item | Why blocked or partial | Manual action |
|---|---|---|
| Two Launcher/Unity GUI clients in one match, including room join, play, spectate, reconnect, and closing the game window mid-match | Console T1-T8 verifies protocol behavior only. Interactive GUI/gameplay was not observed in this run. | Follow [the two-client instructions](../README.md#run-two-gui-clients-on-one-machine) and record actual windows/results. |
| OTP correct/incorrect/expired entry and attempt-limit lockout through the Launcher | Existing smtp4dev mail receipt does not prove code-entry behavior; no GUI OTP attempt was observed. | Use disposable test accounts, inspect smtp4dev locally, enter each case in the Launcher, and capture sanitized app/mail evidence with timestamps. |
| SMTP outage/recovery and bounded queue behavior against a stopped/restarted smtp4dev | No outage transition was exercised in this run. | Only with explicit approval, stop/start `dtp-smtp` (not any other container), observe application responsiveness and eventual delivery, then restore its original running state. |
| Authenticated 10–50 client gameplay/load tests, Clumsy impairment, and full TC11/TC12 recapture for this exact build | The available protocol/load and prior sanitized packet evidence have narrower scopes; this run did not perform an authenticated load or packet capture. | Reuse valid existing evidence only where build/config match; otherwise run bounded tests and record clients, timings, errors, and capture filters. Do not use Clumsy if it requires elevated/system changes. |
| Public Internet deployment | AWS actions are forbidden in this work session; no AWS command or resource was used. | Check current costs/Free Tier, obtain owner approval, then follow [the Internet demo runbook](demo-runbook.md). |
| Final port 9000 cleanup | Port 9000 was already listening under PID 13472 before this work began. It was not started or stopped by this session. | The owner should identify that process and decide whether to stop it. Do not terminate an unknown/pre-existing process. |
| Temporary TLS test certificate cleanup | An encrypted self-signed test PFX was created under the system temp directory, outside the repository, and was not imported into a certificate store. External deletion was intentionally not performed. | Locate the test PFX by its creation time, verify the exact file is the generated test artifact, then remove only that file when it is no longer needed. |
| Generated Unity/Launcher diagram and design verification against live UI | Existing PNG exports are present, but not every control/flow was rechecked against a live two-client session. | Review the `.drawio` sources and exported PNGs against the running application before submission. |

Do not record credentials, OTP values, private keys, or personal account data in
this file.
