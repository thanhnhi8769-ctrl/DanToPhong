# Bằng chứng kiểm thử

Ngày chạy: 2026-11-27. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — không có máy/client thứ hai và tài khoản kiểm thử trong phiên này | Chưa có |
| TC03 | TCP room: tạo/vào phòng và nhận snapshot giữa hai client | BLOCKED — chưa chạy Unity client/hai máy | Chưa có |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | BLOCKED — SMTP credentials chưa được cấu hình trong runtime | Chưa có |
| TC05 | TLS handshake và từ chối client plaintext | BLOCKED — deployment cũ có log FAIL; source/config hiện tại chưa được chạy lại | [TC05-tls-plaintext.log](TC05-tls-plaintext.log) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | BLOCKED — TLS chưa hoạt động; không tạo capture chứa dữ liệu nhạy cảm | Chưa có |
| TC07 | Mất mạng, reconnect và server restart | BLOCKED — không có quyền quản trị từ xa khả dụng (SSM không đăng ký); không thể deploy/cấu hình certificate | Chưa có |
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — build source TCP hiện tại; chưa chạy game hoặc xác minh kết nối server | [TC08-unity-build.log](TC08-unity-build.log) |
| TC09 | Framing TCP: đọc phân mảnh, frame nối liền, EOF, length sai/truncated | PASS — smoke test in-memory của MessageFramer | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | BLOCKED — cần server chạy với DB/config runtime | Chưa có |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | BLOCKED — chưa capture server thật; không tạo pcap giả | Chưa có |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | BLOCKED — TC05 hiện FAIL; cần certificate/deployment TLS hợp lệ | Chưa có |

### Phân tích TC05

Log lịch sử `TC05-tls-plaintext.log` ghi nhận TLS handshake không hoàn tất và probe plaintext nhận `ERR_INVALID_INPUT`. Không có token, mật khẩu, OTP hay dữ liệu tài khoản trong phép thử. Kết quả đó chỉ mô tả deployment cũ; sau các thay đổi source/config, chưa chạy lại đủ bốn ca TLS+TLS, TLS+plaintext, certificate sai và plaintext tường minh. Vì vậy TC05 hiện BLOCKED, không phải PASS.

Các ca BLOCKED chưa được thực hiện; không xem là PASS và không có ảnh/log giả lập.

### TC08 — Unity Windows x64 build

Build bằng Unity Editor 6000.3.28f1 qua `BuildPipeline.BuildPlayer` với target Windows x64. Build hiện tại thành công sau khi gỡ plugin networking, sửa lỗi tên `Random` bị nhập nhằng và xóa trạng thái chỉ được ghi. Không còn cảnh báo C# trong lần build cuối; Unity vẫn ghi một số lỗi không fatal liên quan licensing/package HTTPS. Build thành công không chứng minh gameplay, kết nối server hay hành vi hai máy.

### Phase 4B — kiểm thử giao thức mạng

Chạy kiểm thử framer local bằng lệnh trong [protocol.md](../protocol.md). Smoke
test chỉ kiểm tra `MessageFramer`, không thay thế tải server thật. TC10–TC12
chưa thực hiện; không có file `.pcapng`/ảnh vì endpoint, DB và TLS runtime chưa
sẵn sàng. Khi capture thật, dùng tài khoản/dữ liệu giả và redact IP, email,
OTP, token trước khi lưu.

Đối với TC11/TC12, xem checklist capture trong
[protocol.md](../protocol.md#9-verification-and-capture-guidance).
