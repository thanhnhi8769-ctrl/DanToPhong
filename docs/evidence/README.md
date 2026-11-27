# Bằng chứng kiểm thử

Ngày chạy: 2026-11-27. Chỉ lưu kết quả đã loại endpoint/IP và thông tin xác thực.

| Test ID | Mục tiêu | Kết quả | Bằng chứng |
|---|---|---|---|
| TC01 | Kết nối TCP từ một máy kiểm thử tới cổng 9000 public | PASS — chỉ xác nhận reachability từ máy hiện tại, không đại diện cho hai máy | [TC01-tcp-reachability.log](TC01-tcp-reachability.log) |
| TC02 | Hai máy khác mạng đăng nhập, vào phòng và đồng bộ trạng thái | BLOCKED — đã mở hai cửa sổ GUI đồng thời nhưng chưa có DB/tài khoản và client mạng thứ hai | [TC02-two-client-gui.log](TC02-two-client-gui.log) |
| TC03 | TCP room: tạo/vào phòng và nhận snapshot giữa hai client | BLOCKED — hai GUI chỉ tới main menu; SQL-backed server/gameplay chưa chạy | Chưa có |
| TC04 | OTP SMTP: gửi, nhận, mã đúng/sai/hết hạn | BLOCKED — local SQL runtime không chạy; chưa có SMTP giả trong runtime | Chưa có |
| TC05 | TLS+TLS, TLS+plaintext, certificate sai, plaintext tường minh | BLOCKED — deployment cũ có log FAIL; local server/certificate runtime chưa sẵn sàng để chạy đủ bốn ca | [TC05-tls-plaintext.log](TC05-tls-plaintext.log) |
| TC06 | DPI: so sánh plaintext/TLS bằng Wireshark | BLOCKED — server/TLS runtime và packet capture chưa chạy | Chưa có |
| TC07 | Mất mạng, reconnect và server restart | BLOCKED — chưa có SQL-backed live server; stream retry chưa được kiểm chứng runtime | Chưa có |
| TC08 | Build Unity Windows x64 từ các scene đang bật trong Build Settings | PASS — build source TCP hiện tại; chưa chạy game hoặc xác minh kết nối server | [TC08-unity-build.log](TC08-unity-build.log) |
| TC09 | Framing TCP và thứ tự event/snapshot | PASS — smoke test in-memory kiểm tra frame phân mảnh/nối liền, lỗi frame và wire order GAME_EVENT → GAME_SNAPSHOT | [TC09-framing-smoke.log](TC09-framing-smoke.log) |
| TC10 | 10–50 client, slow sender, abort và malformed traffic tới server thật | BLOCKED — SQL Server/Docker runtime không khả dụng, chưa mở server thật | Chưa có |
| TC11 | TCP 3-way handshake và FIN tới cổng 9000 | BLOCKED — chưa capture server thật; không tạo pcap giả | Chưa có |
| TC12 | TLS handshake/cipher và so sánh plaintext/TLS bằng Wireshark | BLOCKED — TC05 chưa retest; cần server/certificate runtime và capture | Chưa có |

### Phân tích TC05

Log lịch sử `TC05-tls-plaintext.log` ghi nhận TLS handshake không hoàn tất và probe plaintext nhận `ERR_INVALID_INPUT`. Không có token, mật khẩu, OTP hay dữ liệu tài khoản trong phép thử. Kết quả đó chỉ mô tả deployment cũ; sau các thay đổi source/config, chưa chạy lại đủ bốn ca TLS+TLS, TLS+plaintext, certificate sai và plaintext tường minh. Vì vậy TC05 hiện BLOCKED, không phải PASS.

Các ca BLOCKED chưa được thực hiện; không xem là PASS và không có ảnh/log giả lập.

### Điều kiện để gỡ BLOCKED local

- SQL Server: trên máy kiểm thử, quản trị viên cần khởi động instance đã cài
  trong PowerShell elevated bằng
  `Start-Service -Name 'MSSQL$SQLNEW'`, sau đó xác nhận service `Running` và
  cổng DB cấu hình đang lắng nghe. Lần thử hiện tại bị từ chối quyền service;
  không lặp lại thao tác nâng quyền từ agent.
- Nếu dùng Docker thay cho instance đó, cần khởi động Docker Desktop/Engine
  trước khi chạy container; hiện `docker info` không kết nối được daemon.
- Capture/impairment tools chưa cài (`tshark`, Wireshark GUI và `clumsy` đều
  không có trong PATH/các vị trí cài mặc định đã kiểm tra). Nếu được duyệt,
  cài bằng `winget install --id WiresharkFoundation.Wireshark --exact
  --source winget` và `winget install --id Jagt.Clumsy --exact --source winget`.
  Chưa cài hoặc đổi cấu hình driver/network trong phiên này.
- SMTP test: khi Docker Engine sẵn sàng, chạy SMTP test sink ngoài repo, ví dụ
  `docker run --rm -p 2525:25 -p 5000:80 rnwood/smtp4dev`; cấu hình Server
  qua biến môi trường với dữ liệu giả. Không dùng Gmail credential.
- TLS test: tạo certificate test ngoài repo với SAN khớp hostname local và
  cấu hình Server/client qua môi trường; không commit certificate/private key.

### Draw.io

All four `.drawio` files parse as XML. PNG export is BLOCKED because Draw.io
Desktop/CLI is not installed. If approved, install with
`winget install --id JGraph.Draw --exact --source winget`, then export PNGs
from the diagrams.net Desktop UI. Installation was not run because it changes
system software.

### TC08 — Unity Windows x64 build

Build bằng Unity Editor 6000.3.28f1 qua `BuildPipeline.BuildPlayer` với target Windows x64. Build hiện tại thành công sau khi gỡ plugin networking, sửa lỗi tên `Random` bị nhập nhằng và xóa trạng thái chỉ được ghi. Không còn cảnh báo C# trong lần build cuối; Unity vẫn ghi một số lỗi không fatal liên quan licensing/package HTTPS. Build thành công không chứng minh gameplay, kết nối server hay hành vi hai máy.

Clone sạch commit `82b1f5f` cũng build Server Release, chạy protocol smoke,
publish Launcher win-x64 và tạo Unity Windows x64 player thành công. Log đã
redact: [TC08-clean-clone-build.log](TC08-clean-clone-build.log).

### Phase 4B — kiểm thử giao thức mạng

Chạy kiểm thử framer local bằng lệnh trong [protocol.md](../protocol.md). Smoke
test chỉ kiểm tra `MessageFramer`, không thay thế tải server thật. TC10–TC12
chưa thực hiện; không có file `.pcapng`/ảnh vì endpoint, DB và TLS runtime chưa
sẵn sàng. Khi capture thật, dùng tài khoản/dữ liệu giả và redact IP, email,
OTP, token trước khi lưu.

Đối với TC11/TC12, xem checklist capture trong
[protocol.md](../protocol.md#9-verification-and-capture-guidance).
