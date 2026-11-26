# Ghi chú bảo mật mạng

## Kết quả rà soát code

- TCP Server và client dùng frame JSON UTF-8 có length-prefix 4 byte big-endian;
  giới hạn frame 64 KiB. Server đọc đủ header/body, từ chối length sai/truncated;
  Unity client được đồng bộ cùng giới hạn.
- Server xử lý I/O bất đồng bộ, giới hạn 128 session; sau Ctrl+C đóng socket và
  chờ session hoàn tất cleanup. Idle request timeout 2 phút; TLS handshake
  timeout 10 giây. Launcher timeout 12 giây, Unity timeout 8 giây.
- Không có heartbeat ứng dụng hoặc TCP keep-alive cấu hình. Client phát hiện
  lỗi khi có request/timeout hoặc socket bị đóng; idle session được server đóng
  khi hết timeout.
- Mật khẩu DB lưu bằng PBKDF2-HMAC-SHA256 với salt ngẫu nhiên 16 byte và
  600.000 vòng; so sánh hash constant-time. Mật khẩu vẫn đi trong body request
  trước khi được hash tại server.
- Token đăng nhập tạo từ 24 byte ngẫu nhiên, server lưu SHA-256 hash và đặt hạn
  6 giờ. Launcher truyền token cho Unity qua command-line arguments; tiến trình
  cục bộ có quyền đọc command line có thể quan sát token. Cần chuyển sang IPC
  an toàn/inherited handle hoặc credential broker nếu threat model yêu cầu bảo
  vệ token khỏi tiến trình cùng user.
- OTP là 6 chữ số, hash/salt bằng PBKDF2-HMAC-SHA256 120.000 vòng, hết hạn sau
  10 phút, tối đa 5 lần thử, single-use và có cooldown 60 giây. SQL transaction
  với row lock bảo vệ consume khỏi dùng lại đồng thời. Response gửi mã giữ
  thông tin tài khoản ở mức tổng quát.
- Đăng nhập có giới hạn cơ bản 5 request/IP trong cửa sổ 15 phút; đếm thử đồng
  thời, reset sau đăng nhập thành công, lưu trong RAM, tối đa 10.000 IP entry.
  Giới hạn mất khi restart, không chia sẻ giữa nhiều instance, NAT có thể khiến
  nhiều người dùng chung IP bị ảnh hưởng và không thay thế WAF/distributed
  rate-limit.
- Cooldown gửi OTP 60 giây gắn với khóa định danh username/email; chưa có
  quota gửi theo IP/toàn hệ thống. Cần theo dõi chi phí/abuse SMTP và bổ sung
  giới hạn phân tán trước khi mở rộng ra nhiều instance.
- `RoomRegistry`/invites lưu trong RAM. Dữ liệu transform gameplay qua Photon;
  TCP Server không xác thực/kiểm chứng physics snapshot.

## Rủi ro phát hành nghiêm trọng: TLS

`DANTOPHONG_USE_TLS` mặc định `false` ở Server và phía client. Khi tắt TLS,
username/password, OTP, token, hồ sơ và các field JSON được truyền plaintext,
có thể bị đọc hoặc sửa trên đường truyền. Không có mã hóa payload ở tầng ứng
dụng.

TC05 đã ghi nhận endpoint thử nghiệm chấp nhận frame plaintext; TLS handshake
không thành công. Vì vậy không dùng tài khoản thật hoặc coi kết nối Internet
hiện tại là an toàn. Cần cài certificate hợp lệ ngoài repo, bật TLS trên mọi
client/server cùng lúc, kiểm tra hostname/certificate, xác nhận client plaintext
bị từ chối rồi mới chạy TC05/DPI lại.

TLS là server-authentication; chưa yêu cầu client certificate. Không có cơ chế
thương lượng bật TLS: bật lệch cấu hình sẽ khiến handshake/frame thất bại.

## Kiểm thử cần thực hiện

| Ca | Kết quả hiện tại |
|---|---|
| 10–50 client, concurrent session, slow sender và abort | Chưa chạy trên server có DB cấu hình |
| Header/body malformed, length 0/âm/quá 64 KiB qua socket thật | Smoke test framer có; server socket thật chưa test |
| Đóng FIN và cleanup session/room | Code path đã rà soát; chưa có capture/runtime test |
| OTP đúng/sai/hết hạn/5 lần thử và cooldown | Logic có trong code/SQL; SMTP/DB runtime test chưa chạy |
| Brute-force login từ một IP và qua nhiều IP | Rate limiter đã thêm; cần xác minh runtime và giới hạn NAT |
| TLS handshake, certificate sai, plaintext bị từ chối, DPI | Chưa đạt; TC05 hiện FAIL |
| Photon identity binding, gameplay authority/anti-cheat | Chưa triển khai/kiểm thử |

Không lưu password, OTP, token, private key, connection string hoặc packet
capture chứa dữ liệu nhạy cảm vào repo. Chỉ capture bằng tài khoản test/dữ liệu
giả, loại endpoint cá nhân và kiểm tra file trước khi thêm vào
`docs/evidence/`.
