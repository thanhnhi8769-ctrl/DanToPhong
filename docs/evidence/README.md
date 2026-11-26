# Bằng chứng kiểm thử

Ngày chạy: 2026-11-27. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — không có máy/client thứ hai và tài khoản kiểm thử trong phiên này | Chưa có |
| TC03 | Photon: kết nối, tạo/vào phòng và đồng bộ | BLOCKED — chưa chạy Unity client/hai máy | Chưa có |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | BLOCKED — SMTP credentials chưa được cấu hình trong runtime | Chưa có |
| TC05 | TLS handshake và từ chối client plaintext | FAIL — endpoint hiện tại nhận bản tin ứng dụng plaintext; không gửi thông tin đăng nhập | [TC05-tls-plaintext.log](TC05-tls-plaintext.log) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | BLOCKED — TLS chưa hoạt động; không tạo capture chứa dữ liệu nhạy cảm | Chưa có |
| TC07 | Mất mạng, reconnect và server restart | BLOCKED — không có quyền quản trị từ xa khả dụng (SSM không đăng ký); không thể deploy/cấu hình certificate | Chưa có |

### Phân tích TC05

TLS handshake không hoàn tất; probe plaintext riêng biệt nhận phản hồi `ERR_INVALID_INPUT` cho một lệnh không tồn tại. Không gửi token, mật khẩu, OTP hay dữ liệu tài khoản. Kết quả cho thấy listener đang chấp nhận protocol plaintext. Không thể sửa cấu hình runtime từ phiên này vì không có SSM, certificate hoặc file cấu hình bí mật ngoài repo. EC2 và RDS đã được dừng sau phép thử.

Các ca BLOCKED chưa được thực hiện; không xem là PASS và không có ảnh/log giả lập.
