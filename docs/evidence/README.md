# Bằng chứng kiểm thử

Ngày chạy: 2026-11-27. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.
Bản báo cáo tổng hợp chính thức: [BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md](../../BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md).

Checklist ảnh cần chụp cho các ca PARTIAL/BLOCKED:
[screenshot-checklist.md](screenshot-checklist.md). Đây là kế hoạch thao tác,
không phải bằng chứng đã chụp; chỉ ghi ảnh thật sau khi người vận hành thực hiện.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — chưa chạy hai client Unity đã đăng nhập trên hai máy/mạng khác nhau | [TC02-two-client-gui.log](TC02-two-client-gui.log) |
| TC03 | TCP room: tạo/vào phòng và nhận snapshot giữa hai client | BLOCKED — chưa chạy trận hai client; Launcher process không có cửa sổ UI truy cập được trong phiên này | Chưa có |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | PARTIAL — smtp4dev ghi nhận email registration đã gửi/nhận; nhập mã đúng/sai/hết hạn và khóa do thử sai chưa kiểm chứng; không có cửa sổ UI truy cập được để tiếp tục | [TC04-prior-otp-delivery.log](TC04-prior-otp-delivery.log); chưa có ảnh app/mail cho các lần nhập |
| TC05 | TLS+TLS, TLS+plaintext, certificate sai, plaintext tường minh | PASS — 4/4 ca trên loopback 19000 với `DanToPhongDB_Test`; TLS secondary listener 19001 cũng được kiểm chứng | [Latest pinned run 18:47](TC05-pinned-local-20261127-184719.log); [previous pinned run](TC05-pinned-local-20261127-180428.log); [historical run](TC05-local-tls-fail-closed.log) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | PASS — trace local cho thấy PING plaintext đọc được, còn TLS 1.3 application records được mã hóa | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log) |
| TC07 | Mất mạng, reconnect và server restart | BLOCKED — chưa có trận để thử reconnect runtime; Launcher process không có cửa sổ UI truy cập được trong phiên này | Chưa có |
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — build source TCP hiện tại; chưa chạy game hoặc xác minh kết nối server | [TC08-unity-build.log](TC08-unity-build.log) |
| TC09 | Framing TCP, mailbox server, account limits, email queue và thứ tự event/snapshot | PASS — smoke in-memory kiểm tra frame/mailbox, email 100 ký tự/password 8–128, bounded email queue và retry giả lập SMTP 451/550; không phải SMTP delivery test | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | PARTIAL — 50 PING ẩn danh đồng thời, frame phân mảnh, disconnect, JSON lỗi và length quá lớn đều bị xử lý đúng trên loopback; chưa có tải authenticated/gameplay hoặc Clumsy | [TC10-loopback-framing-test.log](TC10-loopback-framing-test.log) |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | PASS — SYN/SYN-ACK và FIN/FIN-ACK quan sát được trên Npcap Loopback | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | PASS — TLS 1.3 ServerHello/cipher và plaintext probe đối chiếu trong trace đã rà payload | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |
| E2 | SMTP worker giao email OTP qua queue bounded | PARTIAL — worker đã giao nội dung tổng hợp vào smtp4dev loopback; chưa kiểm thử outage recovery, Gmail/TLS hoặc nhập OTP trên GUI | [E2-smtp4dev-queue-delivery.log](E2-smtp4dev-queue-delivery.log) |
| E3 | SMTP sample tiếng Việt có attachment và ProtocolLogger redacted | PARTIAL — sample gửi vào smtp4dev loopback; trace ở memory, RedactSecrets bật. AUTH redaction và Gmail/TLS không được kiểm chứng | [E3-smtp-sample.log](E3-smtp-sample.log) |

### Chương 3 — kiểm thử 8 bước

| Test | Kết quả | Bằng chứng |
|---|---|---|
| T1 | PASS — `netstat` thấy listener `127.0.0.1:9000`; chỉ xác nhận loopback. | [T1-T8-chapter3-tests.log](T1-T8-chapter3-tests.log) |
| T2–T8 | PARTIAL — PASS ở mức console/protocol TCP; kiểm thử Launcher/Unity GUI vẫn cần chạy tay. | [Latest console run 18:47](T2-T8-protocol-console-20261127-184734.log); [previous run](T2-T8-protocol-console-20261127-182612.log); [lịch sử GUI blocker](T1-T8-chapter3-tests.log) |

Harness chạy hai server test ngắn hạn trên loopback với plaintext bật tường minh, dùng tài khoản tạm trong `DanToPhongDB_Test` rồi xóa chúng. Kết quả không chứng minh TLS (TC05 có log riêng), GUI rendering hay thao tác nút đóng.

### Phân tích TC05

`TC05-pinned-local-20261127-184719.log` records all four current transport cases against a self-signed localhost certificate whose SHA-256 pin was supplied only to the test process. It also verifies the opt-in TLS secondary listener on 19001. The test server used only the isolated `DanToPhongDB_Test` and closed after testing; TLS evidence is loopback-only, not GUI login, Internet reachability, or packet-capture evidence. Earlier TC05 logs remain linked above for historical context.

### Fresh local build and smoke checks

- [local-build-smoke-20261127-184655.log](local-build-smoke-20261127-184655.log): Server Release build (0 warnings/errors), Launcher Release build (0 warnings/errors), protocol smoke, and synthetic SMTP4dev sample all exited successfully. The default queue test observed simulated SMTP 451/550 retry then success; the SMTP sample was accepted by local smtp4dev. `ProtocolLogger.RedactSecrets` was enabled; its trace stayed in memory.
- The latest [pinned TC05 run](TC05-pinned-local-20261127-184719.log) and [T2–T8 protocol run](T2-T8-protocol-console-20261127-184734.log) above were rerun against the test database. All created test servers were stopped and temporary harness accounts were removed. The T2–T8 run remains protocol-level evidence, not GUI evidence.

Các mục PARTIAL/BLOCKED chưa được thực hiện đầy đủ; không xem là PASS và không có ảnh/log giả lập.

### Trạng thái môi trường local

- Docker `dtp-sql` and `dtp-smtp` are running. Server initialized only the
  dedicated `DanToPhongDB_Test`; the SQL password and connection string are
  supplied through process environment variables and are not recorded.
- Existing smtp4dev registration messages are documented in
  [TC04-prior-otp-delivery.log](TC04-prior-otp-delivery.log). No message was
  resent; entering correct/incorrect/expired codes and the attempt limit remain
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
