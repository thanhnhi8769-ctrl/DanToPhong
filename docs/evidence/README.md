# Bằng chứng kiểm thử

Ngày chạy: 2026-11-27. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — chưa chạy hai client Unity đã đăng nhập trên hai máy/mạng khác nhau | [TC02-two-client-gui.log](TC02-two-client-gui.log) |
| TC03 | TCP room: tạo/vào phòng và nhận snapshot giữa hai client | BLOCKED — server đã chạy với DB test, nhưng chưa chạy hai client trong một trận thật | Chưa có |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | PARTIAL — smtp4dev ghi nhận email registration đã gửi/nhận; nhập mã đúng/sai/hết hạn và khóa do thử sai chưa kiểm chứng | [TC04-prior-otp-delivery.log](TC04-prior-otp-delivery.log); chưa có ảnh app/mail cho các lần nhập |
| TC05 | TLS+TLS, TLS+plaintext, certificate sai, plaintext tường minh | PASS — 4/4 ca quan sát trực tiếp trên loopback TCP 9000 với database test riêng | [TC05-local-tls-fail-closed.log](TC05-local-tls-fail-closed.log); [TC05-tls-plaintext.log](TC05-tls-plaintext.log) (lịch sử deployment cũ, không dùng làm kết quả hiện tại) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | PASS — trace local cho thấy PING plaintext đọc được, còn TLS 1.3 application records được mã hóa | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log) |
| TC07 | Mất mạng, reconnect và server restart | BLOCKED — live server sẵn sàng nhưng reconnect giữa trận và khôi phục snapshot chưa được thử runtime | Chưa có |
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — build source TCP hiện tại; chưa chạy game hoặc xác minh kết nối server | [TC08-unity-build.log](TC08-unity-build.log) |
| TC09 | Framing TCP và thứ tự event/snapshot | PASS — smoke test in-memory kiểm tra frame phân mảnh/nối liền, lỗi frame và wire order GAME_EVENT → GAME_SNAPSHOT | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | BLOCKED — chưa chạy load/slow-sender/malformed test trên server thật | Chưa có |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | PASS — SYN/SYN-ACK và FIN/FIN-ACK quan sát được trên Npcap Loopback | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | PASS — TLS 1.3 ServerHello/cipher và plaintext probe đối chiếu trong trace đã rà payload | [TC06-TC11-TC12-loopback-capture.log](TC06-TC11-TC12-loopback-capture.log); [TC11-TC12-loopback-test.pcapng](TC11-TC12-loopback-test.pcapng) |

### Phân tích TC05

`TC05-local-tls-fail-closed.log` records all four current local transport cases. The older `TC05-tls-plaintext.log` describes a failed TLS attempt against a previous deployment and is retained only as historical context. The current PASS is limited to loopback transport behavior; GUI authentication, Internet reachability and packet-capture analysis are separate tests.

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

### Draw.io

Draw.io Desktop is installed (32.7.4.0). All four `.drawio` files parse as XML;
PNG export has not yet been verified in this run.

### TC08 — Unity Windows x64 build

Build bằng Unity Editor 6000.3.28f1 qua `BuildPipeline.BuildPlayer` với target Windows x64. Build hiện tại thành công sau khi gỡ plugin networking, sửa lỗi tên `Random` bị nhập nhằng và xóa trạng thái chỉ được ghi. Không còn cảnh báo C# trong lần build cuối; Unity vẫn ghi một số lỗi không fatal liên quan licensing/package HTTPS. Build thành công không chứng minh gameplay, kết nối server hay hành vi hai máy.

Clone sạch commit `82b1f5f` cũng build Server Release, chạy protocol smoke,
publish Launcher win-x64 và tạo Unity Windows x64 player thành công. Log đã
redact: [TC08-clean-clone-build.log](TC08-clean-clone-build.log).

### Phase 4B — kiểm thử giao thức mạng

TC09 remains an in-memory framing smoke test, not a server-load test. TC11 and
TC12 use a sanitized Npcap Loopback capture with anonymous traffic; TC10 load,
slow-sender and malformed-input cases remain untested. Any later capture must
exclude or redact account data, email, OTPs, tokens and non-loopback addresses.

Đối với TC11/TC12, xem checklist capture trong
[protocol.md](../protocol.md#9-verification-and-capture-guidance).
