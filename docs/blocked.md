# Blocked and manual verification items

Only items not demonstrated in this work session are listed here. Reclassify
them only after observing the result and saving sanitized evidence.
Current aggregate status and the read-only AWS snapshot are in
[BAO_CAO_DO_AN_NHAP_MON_MANG.md](../BAO_CAO_DO_AN_NHAP_MON_MANG.md).

| Item | Why blocked or partial | Manual action |
|---|---|---|
| Two Launcher/Unity GUI clients in one match, including room join, play, spectate, reconnect, and closing the game window mid-match | Protocol integration verified room/snapshot/input/spectate/reconnect; interactive GUI/gameplay and window-close behavior were not observed. | Follow [the two-client instructions](../README.md#run-two-gui-clients-on-one-machine) and record actual windows/results. |
| OTP correct/incorrect/expired entry and attempt-limit lockout through the Launcher | DB-backed tests verified wrong/expired/single-use code state and lockout, but did not exercise SMTP-to-Launcher entry; no GUI OTP attempt was observed. | Use disposable test accounts, inspect smtp4dev locally, enter each case in the Launcher, and capture sanitized app/mail evidence with timestamps. |
| SMTP outage/recovery and bounded queue behavior against a stopped/restarted smtp4dev | No outage transition was exercised in this run. | Only with explicit approval, stop/start `dtp-smtp` (not any other container), observe application responsiveness and eventual delivery, then restore its original running state. |
| Authenticated 10–50 client gameplay/load tests, Clumsy impairment, and full TC11/TC12 recapture for this exact build | A bounded 50-anonymous-client test passed (p95 3.6 ms, max 10.6 ms) with a slow sender and malformed/oversized frames; authenticated/gameplay load, Clumsy and new packet capture were not run. | Reuse valid existing evidence only where build/config match; otherwise run bounded tests and record clients, timings, errors, and capture filters. Do not use Clumsy if it requires elevated/system changes. |
| Public Internet deployment / TC02 | Read-only check succeeded, but EC2 and RDS are stopped. No start/stop or network mutation was authorized; RDS reports `PubliclyAccessible=True` and needs owner review. | Check current cost/Free Tier and obtain explicit owner approval. Re-run `.\scripts\aws-test-helper.ps1 -Action Status`, then follow [the Internet demo runbook](demo-runbook.md); use only approved `/32` ingress and do not move the RDS-associated EIP. |
| Temporary TLS test certificate cleanup | Multiple self-signed PFX files from TC05 and two local-run startup checks remain under system temp, outside the repository; none was imported. One older TC05 artifact used an empty export password. | Enumerate `DanToPhong-tc05-*.pfx` and `DanToPhong-local-*.pfx`, verify each exact path against the run times/output, then remove only individually verified test artifacts when no longer needed. |
| Generated Unity/Launcher diagram and design verification against live UI | Existing PNG exports are present, but not every control/flow was rechecked against a live two-client session. | Review the `.drawio` sources and exported PNGs against the running application before submission. |

Do not record credentials, OTP values, private keys, or personal account data in
this file.
