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
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — player build hoàn tất; chưa chạy game hoặc xác minh kết nối server | [TC08-unity-build.log](TC08-unity-build.log) |
| TC09 | Framing TCP: đọc phân mảnh, frame nối liền, EOF, length sai/truncated | PASS — smoke test in-memory của MessageFramer | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | BLOCKED — cần server chạy với DB/config runtime | Chưa có |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | BLOCKED — chưa capture server thật; không tạo pcap giả | Chưa có |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | BLOCKED — TC05 hiện FAIL; cần certificate/deployment TLS hợp lệ | Chưa có |

### Phân tích TC05

TLS handshake không hoàn tất; probe plaintext riêng biệt nhận phản hồi `ERR_INVALID_INPUT` cho một lệnh không tồn tại. Không gửi token, mật khẩu, OTP hay dữ liệu tài khoản. Kết quả cho thấy listener đang chấp nhận protocol plaintext. Không thể sửa cấu hình runtime từ phiên này vì không có SSM, certificate hoặc file cấu hình bí mật ngoài repo. EC2 và RDS đã được dừng sau phép thử.

Các ca BLOCKED chưa được thực hiện; không xem là PASS và không có ảnh/log giả lập.

### TC08 — Unity Windows x64 build

Build bằng Unity Editor 6000.3.28f1 qua `BuildPipeline.BuildPlayer` với target Windows x64. Player được tạo trong thư mục `BuildGameRelease`, vốn bị `.gitignore` loại trừ. Script compilation hoàn tất; còn hai cảnh báo `CS0414` về field chưa dùng trong `HelpSystem` và `scooter`. Build thành công không chứng minh game khởi chạy, gameplay hoạt động hoặc server/Photon có thể kết nối; các kiểm thử runtime này vẫn cần thực hiện riêng.

### Phase 4B — kiểm thử giao thức mạng

Chạy kiểm thử framer local bằng lệnh trong [protocol.md](../protocol.md). Smoke
test chỉ kiểm tra `MessageFramer`, không thay thế tải server thật. TC10–TC12
chưa thực hiện; không có file `.pcapng`/ảnh vì endpoint, DB và TLS runtime chưa
sẵn sàng. Khi capture thật, dùng tài khoản/dữ liệu giả và redact IP, email,
OTP, token trước khi lưu.

Đối với TC11/TC12, xem checklist filter, tên file và nội dung caption trong
[protocol.md](../protocol.md#7-checklist-capture-wireshark-chua-co-capture-trong-repo).
