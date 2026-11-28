# Bằng chứng kiểm thử

Cập nhật: 2026-11-28. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.
Bản báo cáo tổng hợp chính thức: [BAO_CAO_DO_AN_NHAP_MON_MANG.md](../../BAO_CAO_DO_AN_NHAP_MON_MANG.md).

### Get Code connectivity diagnosis and live verification — 2026-11-28

The Launcher had stale user-scope environment overrides that took precedence
over the endpoint and TLS settings in the packaged `server.json`. The client
now treats explicit JSON values as authoritative and environment variables as
fallbacks. Transport errors are surfaced as connection errors, not SMTP mail
errors. No machine environment variables or AWS security-group rules were
changed.

- Release build and protocol smoke: [build log](local-mail-connect-fix-build-20261128-0225.log),
  [smoke log](local-mail-fix-protocol-smoke-20261128-final.log).
- Refreshed friend package: `../../artifacts/DanToPhong-friends-win-x64.zip`;
  archive contents, Launcher/Unity executables, and TLS endpoint JSON were
  checked locally. Live TLS hostname/pin validation passed.
- During the earlier approved AWS run, a separate EC2 Elastic IP was assigned without
  moving the existing RDS EIP or changing Security Groups. The certificate was
  rotated for the EC2 endpoint; current Server Release was deployed with a
  rollback copy retained. At that time, read-only checks confirmed EC2 running,
  RDS available, systemd active, SQL connected, and TCP 9000/UDP 9001 listening.
- One live Get Code request, sent before the Server Release deployment, was
  accepted over verified TLS; the user confirmed receipt of the test email.
  No duplicate was sent after deployment. OTP entry/verification through the
  Launcher and two-network multiplayer remain unverified. See the
  [sanitized AWS diagnosis](aws-mail-connectivity-diagnosis-20261128.log).
- The latest read-only check on 2026-11-28 reports both EC2 and the existing
  RDS stopped. No AWS resources were started or modified during the local
  validation described below. Security-group inspection detected no public
  `0.0.0.0/0` or `::/0` ingress for TCP 9000, UDP 9001, or TCP 1433.
- After the product fixes, the Unity Windows x64 player and self-contained
  Launcher package were rebuilt locally from the current source. The 496-entry
  archive was checked for both executables and its TLS-enabled TCP 9000/UDP
  9001 config matched the previously verified pin. AWS was stopped, so this
  package check did not validate a live endpoint. The package publish had six
  generated Designer nullable-context warnings and no errors.
  [Unity player build](local-unity-player-build-20261128.log);
  [package build](local-friends-package-20261128.log).
- The source and deployment templates were subsequently changed to use UDP
  9000 (alongside TCP/TLS 9000). The earlier AWS listener/package evidence above
  reflects the previously deployed UDP 9001 configuration; no AWS service,
  security group, or package deployment was changed as part of this local port
  update. UDP 9000 runtime reachability remains unverified.

Snapshot AWS read-only lúc 20:05 (+07), 27/11/2026 là lịch sử trước lần khởi
động được duyệt ngày 28/11; snapshot đó ghi EC2/RDS đang dừng:
[log đã lược ID/IP](aws-status-readonly-20261127-2005.log). Trạng thái sau lần
khởi động và triển khai được mô tả trong phần Get Code ở đầu tài liệu. Trạng
thái hiện tại được xác nhận read-only ngày 28/11: cả EC2 và RDS đều dừng.
Không thay đổi Security Group trong lần kiểm tra này.

Checklist ảnh cần chụp cho các ca PARTIAL/BLOCKED:
[screenshot-checklist.md](screenshot-checklist.md). Đây là kế hoạch thao tác,
không phải bằng chứng đã chụp; chỉ ghi ảnh thật sau khi người vận hành thực hiện.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — chưa chạy hai client Unity đã đăng nhập trên hai máy/mạng khác nhau | [TC02-two-client-gui.log](TC02-two-client-gui.log) |
| TC03 | Hybrid room/gameplay: 4 racer UDP input và TCP snapshot | PARTIAL — DB-backed harness xác nhận bốn racer tạo/join room, gửi HMAC/sequence UDP input; tất cả bốn vị trí xuất hiện trong TCP snapshots, thêm spectator nhận live state. Unity GUI/Internet chưa xác minh | [Latest integration](local-protocol-integration-20261128-135041.log) |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | PARTIAL — một Get Code live qua TLS đã được người dùng xác nhận nhận email; DB-backed harness kiểm sai/hết hạn/single-use/khóa sau 5 lần, login limiter, email persistence và từ chối email khôi phục không khớp; chưa nhập mã qua Launcher | [AWS diagnosis](aws-mail-connectivity-diagnosis-20261128.log); [latest integration](local-protocol-integration-20261128-135041.log) |
| TC05 | TLS+TLS, TLS+plaintext, certificate sai, plaintext tường minh | PASS — pinned TLS, pin mismatch reject, plaintext reject/explicit trên loopback; không phải Internet test | [Latest pinned run 21:20](TC05-pinned-local-20261127-212014.log); [previous pinned run](TC05-pinned-local-20261127-211418.log); [historical local run](TC05-local-tls-fail-closed.log); [older remote failure — not current build evidence](TC05-tls-plaintext.log) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | PASS — trace local cho thấy PING plaintext đọc được, còn TLS 1.3 application records được mã hóa | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log) |
| TC07 | Mất mạng, reconnect và server restart | PARTIAL — protocol harness xác minh abrupt reset, reconnect trong grace period và room RAM mất sau process restart; GUI chưa xác minh | [Latest integration](local-protocol-integration-20261128-135041.log) |
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — Unity Windows x64 player được build thành công sau cập nhật UDP channel; chưa chạy GUI/gameplay hoặc xác minh kết nối server | [Latest Unity player build](local-unity-hybrid-build-20261127-2200.log); [Unity solution Release build](local-unity-solution-release-build-20261127-2210.log) |
| TC09 | Framing TCP, mailbox server, account limits, email queue và thứ tự event/snapshot | PASS — smoke in-memory kiểm tra frame/mailbox, email 100 ký tự/password 8–128, bounded email queue và retry giả lập SMTP 451/550; không phải SMTP delivery test | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | PARTIAL — 50 anonymous clients; p95 1.2 ms/max 5.6 ms cùng slow sender giữ header chưa hoàn tất; malformed JSON/oversized header bị đóng. Chưa authenticated/gameplay sustained load, UDP loss/reordering hoặc Clumsy | [Latest integration](local-protocol-integration-20261128-135041.log); [TC10-loopback-framing-test.log](TC10-loopback-framing-test.log) |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | PASS — SYN/SYN-ACK và FIN/FIN-ACK quan sát được trên Npcap Loopback | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | PASS — TLS 1.3 ServerHello/cipher và plaintext probe đối chiếu trong trace đã rà payload | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |
| E2 | SMTP worker giao email OTP qua queue bounded | PARTIAL — synthetic queue retry/delivery smoke và một email Gmail live được xác nhận nhận; outage recovery/GUI OTP chưa kiểm thử | [E2-smtp4dev-queue-delivery.log](E2-smtp4dev-queue-delivery.log); [AWS diagnosis](aws-mail-connectivity-diagnosis-20261128.log) |
| E3 | SMTP sample tiếng Việt có attachment và ProtocolLogger redacted | PARTIAL — sample gửi vào smtp4dev loopback; trace ở memory, RedactSecrets bật. AUTH redaction và Gmail/TLS không được kiểm chứng | [E3-smtp-sample.log](E3-smtp-sample.log) |

### Chương 3 — kiểm thử 8 bước

| Test | Kết quả | Bằng chứng |
|---|---|---|
| T1 | PASS — `netstat` thấy listener `127.0.0.1:9000`; chỉ xác nhận loopback. | [T1-T8-chapter3-tests.log](T1-T8-chapter3-tests.log) |
| T2–T8 | PARTIAL — protocol console passed; four clients authenticated, room/snapshot, UDP movement, chat, reconnect and server shutdown paths passed; recovery mismatch, password reuse rejection and protected avatar persistence also passed. GUI still requires manual checks. | [Latest integration run 13:50](local-protocol-integration-20261128-135041.log); [previous console run](T2-T8-protocol-console-20261127-184734.log); [lịch sử GUI blocker](T1-T8-chapter3-tests.log) |

Harness chạy các server test ngắn hạn trên loopback: cổng riêng 19000, plaintext bật tường minh, tài khoản tạm trong `DanToPhongDB_Test` được xóa sau test. Kết quả không chứng minh GUI rendering hoặc thao tác nút đóng.

The latest integration run also exercises the `SelectedMap` startup migration:
legacy aliases and unsupported values are normalized, the database default is
verified as `game1`, and temporary test accounts are removed. This is database
evidence only, not a Unity scene or GUI playthrough.

### Phân tích TC05

`TC05-pinned-local-20261127-212014.log` records pinned TLS success, mismatch rejection, plaintext rejection, explicit plaintext mode, and the optional secondary TLS listener on loopback. Its ephemeral certificate and pin were supplied only to the test process and the certificate was removed after testing. The run is not GUI login, Internet reachability, or packet-capture evidence. Earlier TC05 logs remain linked above for historical context.

The older remote smoke `TC05-tls-plaintext.log` recorded a TLS handshake
failure and plaintext response from an unidentified remote runtime. Its deployed
binary/configuration could not be inspected, so it is retained as a historical
failure and is not evidence that the current source passed remote TLS.

### Fresh local build and smoke checks

- The 2026-11-28 migration regression passed against `DanToPhongDB_Test`:
  server startup normalized all legacy/unsupported selected-map values and
  restored the canonical `game1` default. See
  [integration log](local-protocol-integration-20261128-135041.log).

- [local-release-build-20261127-211949.log](local-release-build-20261127-211949.log):
  latest Release build for Unity-generated solution, Server, and WinForms
  Launcher. All had 0 errors; Unity and Server had 0 warnings, and Launcher
  had 6 generated-Designer warnings.
- After the Settings persistence fix, the Server Release build completed with
  0 warnings/errors and the Launcher Release build completed with 0 errors and
  6 generated-Designer `CS8669` warnings:
  [Server](local-settings-build-server-20261127-212653.log),
  [Launcher](local-settings-build-ui-20261127-212653.log).
- After restoring the generated Unity project assets, the Unity-generated
  solution build (`-c Release`), Server Release and WinForms Launcher Release
  all passed. Generated Unity project output is under `Temp\bin\Debug`; Unity
  emitted 7 `CS8632`/`CS0649` warnings; Server had 0 warnings/errors; Launcher
  had 6 generated-Designer `CS8669` warnings and 0 errors.
  [Earlier build log](local-release-build-20261127-2115.log).
- [local-protocol-smoke-20261127-212006.log](local-protocol-smoke-20261127-212006.log):
  protocol smoke passed fragmented/coalesced frames, malformed/oversized and
  truncated frames, event/snapshot order, forged identity handling, mailbox
  backpressure, account limits, certificate pin checks, and synthetic SMTP
  retry behavior.
- [local-protocol-integration-20261127-212721.log](local-protocol-integration-20261127-212721.log):
  DB-backed OTP/login limiter checks, two-client room/snapshot/input, spectate,
  reconnect, restart semantics, authenticated account settings and token
  revocation, 50-client bounded load, slow sender, malformed JSON and oversized
  frame checks passed. Temporary test rows were removed.
- [local-protocol-integration-20261127-214844.log](local-protocol-integration-20261127-214844.log):
  repeat DB-backed integration after adding hybrid gameplay; four authenticated
  racers bound independent UDP HMAC sessions, sent increasing movement input,
  and all four server-authoritative positions were observed in ordered TCP
  snapshots. An additional client spectated. OTP, settings, reconnect/restart,
  50-client anonymous framing/load and cleanup checks also passed; TC10 p95
  was 3.4 ms and max 8.0 ms. This does not prove Unity GUI behavior, UDP
  impairment handling or Internet operation.
- [local-protocol-integration-20261128-043514.log](local-protocol-integration-20261128-043514.log):
  latest DB-backed integration run; added explicit recovery-email mismatch,
  password-reuse rejection, and avatar upload authentication/persistence
  checks. Four UDP racers and the spectator, OTP state, reconnect/restart,
  chat, and bounded framing checks passed. TC10 p95 was 1.5 ms and max 6.9 ms.
  This is protocol evidence, not GUI or cross-network evidence.
- [local-unity-solution-release-20261128.log](local-unity-solution-release-20261128.log),
  [local-server-release-20261128.log](local-server-release-20261128.log),
  [local-launcher-release-20261128.log](local-launcher-release-20261128.log),
  and [local-protocol-smoke-20261128.log](local-protocol-smoke-20261128.log):
  current Release builds all passed with 0 warnings and 0 errors; protocol
  smoke includes the four-player limit and Start readiness gate.
- [local-unity-hybrid-build-20261127-2200.log](local-unity-hybrid-build-20261127-2200.log):
  Unity Windows x64 player build completed successfully with the UDP movement
  channel included. The log is verbose; its final build result is Success.
- [local-unity-solution-restore-20261127-2210.log](local-unity-solution-restore-20261127-2210.log)
  and [local-unity-solution-release-build-20261127-2210.log](local-unity-solution-release-build-20261127-2210.log):
  NuGet restore enabled the Unity-generated solution Release build; 0 errors,
  7 existing `CS8632`/`CS0649` warnings.
- [local-hybrid-release-build-20261127-2205.log](local-hybrid-release-build-20261127-2205.log):
  Server and WinForms Launcher Release builds passed with 0 warnings/errors.
  The first Unity-generated solution attempt in this combined run lacked
  restored NuGet assets; the separately linked restore and successful build
  record immediately above resolve that validation prerequisite.
- [local-final-validation-20261127-194616.log](local-final-validation-20261127-194616.log):
  Server Release build passed with 0 warnings/errors; Launcher Release build
  passed with 0 errors and 6 generated-Designer `CS8669` warnings. Protocol
  smoke, synthetic smtp4dev sample, and queued synthetic OTP delivery passed.
  No DB-backed account workflow or GUI was run.
- [local-build-smoke-20261127-184655.log](local-build-smoke-20261127-184655.log): Server Release build (0 warnings/errors), Launcher Release build (0 warnings/errors), protocol smoke, and synthetic SMTP4dev sample all exited successfully. The default queue test observed simulated SMTP 451/550 retry then success; the SMTP sample was accepted by local smtp4dev. `ProtocolLogger.RedactSecrets` was enabled; its trace stayed in memory.
- The [pinned TC05 run](TC05-pinned-local-20261127-212014.log) verifies transport separately. Integration remains protocol-level evidence, not GUI, SMTP delivery, or cross-network evidence.

Các mục PARTIAL/BLOCKED chưa được thực hiện đầy đủ; không xem là PASS và không có ảnh/log giả lập.

### Trạng thái môi trường local

- Docker `dtp-sql` and `dtp-smtp` were available for the integration run. Server initialized only the
  dedicated `DanToPhongDB_Test`; the SQL password and connection string are
  supplied through process environment variables and are not recorded.
- Existing smtp4dev registration messages are documented in
  [TC04-prior-otp-delivery.log](TC04-prior-otp-delivery.log). A separate live
  AWS Get Code email was confirmed received; the address and OTP were not stored.
  Entering correct/incorrect/expired codes through the Launcher remains
  unverified.
- Wireshark's `tshark.exe` is available at
  `C:\Program Files\Wireshark\tshark.exe`; Npcap Loopback capture is verified.
  Clumsy is installed, but network-impairment tests are still pending.
- A short-lived TLS test certificate was created outside the repository. It
  was not imported into a system certificate store.
- The local Launcher process remained responsive, but no UI window was exposed
  to Computer Use. No second launch or interactive gameplay/OTP attempt was
  made; `LauncherRelease/server.json` is ignored by Git.

### Draw.io

Draw.io Desktop 32.7.4.0 exported all four diagrams to PNG:
[architecture](../diagrams/architecture.png),
[application flow](../diagrams/application-flow.png),
[GUI wireframe](../diagrams/gui-wireframe.png), and
[user stories](../diagrams/user-stories.png). The architecture and user-story
diagrams identify simplified gameplay and the remaining unverified behavior.

### TC08 — Unity Windows x64 build

Build bằng Unity Editor 6000.3.28f1 qua `BuildPipeline.BuildPlayer` với target Windows x64. Build hiện tại thành công sau khi gỡ plugin networking, sửa lỗi tên `Random` bị nhập nhằng và xóa trạng thái chỉ được ghi. Không còn cảnh báo C# trong lần build cuối; Unity vẫn ghi một số lỗi không fatal liên quan licensing/package HTTPS. Build thành công không chứng minh gameplay, kết nối server hay hành vi hai máy.

Clone sạch commit `82b1f5f` cũng build Server Release, chạy protocol smoke,
publish Launcher win-x64 và tạo Unity Windows x64 player thành công. Log đã
redact: [TC08-clean-clone-build.log](TC08-clean-clone-build.log).

### Phase 4B — kiểm thử giao thức mạng

TC09 remains an in-memory framing smoke test, not a server-load test. TC10's
loopback probes use anonymous PING only and are not an authenticated load test.
TC11 and TC12 use a sanitized Npcap Loopback capture with anonymous traffic.
Any later capture must exclude or redact account data, email, OTPs, tokens and
non-loopback addresses.

Đối với TC11/TC12, xem checklist capture trong
[protocol.md](../protocol.md#9-verification-and-capture-guidance).
