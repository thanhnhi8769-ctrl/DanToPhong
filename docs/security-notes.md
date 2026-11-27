# Ghi chú bảo mật mạng

## Kết quả rà soát code

- TCP Server và client dùng frame JSON UTF-8 có length-prefix 4 byte big-endian;
  giới hạn frame 64 KiB. Server đọc đủ header/body, từ chối length sai/truncated;
  Unity client được đồng bộ cùng giới hạn.
- Server xử lý I/O bất đồng bộ, giới hạn 128 session; sau Ctrl+C đóng socket và
  chờ session hoàn tất cleanup. Idle request timeout 75 giây; TLS handshake
  timeout 10 giây. Launcher timeout 12 giây, Unity timeout 8 giây.
- Launcher/Unity gửi `PING` mỗi 20 giây khi có phiên đăng nhập; server chỉ trả
  `PONG` cho session đã xác thực. TCP keep-alive bật ở cả client và server.
- Mật khẩu DB lưu bằng PBKDF2-HMAC-SHA256 với salt ngẫu nhiên 16 byte và
  600.000 vòng; so sánh hash constant-time. Mật khẩu vẫn đi trong body request
  trước khi được hash tại server.
- Token đăng nhập tạo từ 24 byte ngẫu nhiên, server lưu SHA-256 hash và đặt hạn
  6 giờ. Launcher truyền token trong environment riêng của tiến trình Unity,
  không đưa vào command line; Unity xóa biến sau khi đọc. Token vẫn có thể bị
  tiến trình cùng user/debugger hoặc công cụ quản trị đọc, nên đây không phải
  ranh giới bảo vệ chống local administrator.
- OTP là 6 chữ số, hash/salt bằng PBKDF2-HMAC-SHA256 120.000 vòng, hết hạn sau
  10 phút, tối đa 5 lần thử, single-use và có cooldown 60 giây. SQL transaction
  với row lock bảo vệ consume khỏi dùng lại đồng thời. Response gửi mã giữ
  thông tin tài khoản ở mức tổng quát.
- Đăng nhập giới hạn 5 lần theo cặp account/IP trong cửa sổ 15 phút; khóa 15
  phút, hash key trước khi lưu trong SQL và xóa trạng thái khi đăng nhập thành
  công. Trạng thái sống qua restart và dùng chung giữa các server đọc cùng DB;
  chưa có giới hạn theo account trên nhiều IP hoặc toàn cục theo IP, nên chưa
  chống được password spraying/distributed brute force.
- Cooldown gửi OTP 60 giây gắn với khóa định danh username/email; chưa có
  quota gửi theo IP/toàn hệ thống. Cần theo dõi chi phí/abuse SMTP và bổ sung
  giới hạn phân tán trước khi mở rộng ra nhiều instance.
- Room/invite/game registry lưu trong RAM. Gameplay dùng kết nối TCP riêng để
  server push snapshot và event có thứ tự; client giữ tối đa 8 snapshot và bỏ
  mẫu cũ nhất khi đầy. Kinematics phía server còn đơn giản; chưa xác thực va
  chạm đường đua hoặc checkpoint theo vị trí.

## TLS fail-closed

`DANTOPHONG_USE_TLS` mặc định `true`; client TLS bắt buộc có thumbprint SHA-256
64 ký tự hex trong `server.json` (`certificateThumbprintSha256`), tính trên DER
của leaf certificate. Launcher chuyển pin public sang Unity qua environment
của tiến trình con, không đưa vào command line. SslStream vẫn kiểm hostname và
thời hạn certificate; chỉ chain có lỗi duy nhất `UntrustedRoot` mới được chấp
nhận nếu pin khớp. Pin thiếu/sai, hostname mismatch, certificate hết hạn hoặc
lỗi chain khác đều bị từ chối. Khi đổi certificate, phải cập nhật pin client
đồng bộ.

Server từ chối khởi động nếu certificate PKCS#12 thiếu hoặc không hợp lệ. TLS
handshake xảy ra trước frame ứng dụng nên plaintext bị đóng. TC05 đã PASS 4/4
trên database test cô lập, loopback port 19000; listener TLS phụ port 19001
cũng được xác minh. Xem [bằng chứng TC05 pinned TLS](evidence/TC05-pinned-local-20261127-180428.log).
Khi `DANTOPHONG_USE_TLS=false`, password/OTP/token là plaintext; chỉ dùng dữ
liệu giả trong thử nghiệm loopback cô lập. TLS chỉ xác thực server, không yêu
cầu client certificate. `DANTOPHONG_SECONDARY_PORT` tùy chọn và tắt mặc định.

## Kiểm thử cần thực hiện

| Ca | Kết quả hiện tại |
|---|---|
| 10–50 client, concurrent session, slow sender và abort | Chưa chạy trên server có DB cấu hình |
| Header/body malformed, length 0/âm/quá 64 KiB qua socket thật | Smoke test framer có; server socket thật chưa test |
| Đóng FIN và cleanup session/room | Code path đã rà soát; chưa có capture/runtime test |
| OTP đúng/sai/hết hạn/5 lần thử và cooldown | Logic có trong code/SQL; SMTP/DB runtime test chưa chạy |
| Brute-force login theo account+IP, lock/unlock qua restart | SQL limiter đã thêm; cần chạy script runtime với DB local |
| TLS handshake, certificate sai, plaintext bị từ chối, DPI | Default fail-closed đã thêm; ca runtime còn BLOCKED |
| Gameplay push stream, event ordering, snapshot queue | Đã thêm trong source; smoke test wire-format có, chưa chạy live server |
| Gameplay authority, kiểm chứng checkpoint theo geometry, spectator, resume | Chưa hoàn thiện/kiểm thử |

Không lưu password, OTP, token, private key, connection string hoặc packet
capture chứa dữ liệu nhạy cảm vào repo. Chỉ capture bằng tài khoản test/dữ liệu
giả, loại endpoint cá nhân và kiểm tra file trước khi thêm vào
`docs/evidence/`.

Không có thư viện mạng bên thứ ba trong cây mã nguồn. Đường TCP gameplay đã có
server-push stream nhưng mô phỏng chuyển động còn đơn giản, spectator và
reconnect bền vững chưa có, và chưa kiểm chứng vật lý đường đua. Đây là giới
hạn chức năng đã biết, không phải PASS.
