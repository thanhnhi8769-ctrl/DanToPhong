# Dân Tổ Phóng

Game đua scooter Unity có chế độ Solo và Multiplayer (Photon PUN), kèm WinForms Launcher, TCP service cổng 9000 và SQL Server. Launcher xác thực tài khoản; game nhận token phiên qua tham số tiến trình. Photon vận chuyển trạng thái trận, còn TCP service xử lý tài khoản và dữ liệu lưu trữ.

## Mục lục

1. [Kiến trúc](#1-kiến-trúc)
2. [Yêu cầu và chạy cục bộ](#2-yêu-cầu-và-chạy-cục-bộ)
3. [Build Launcher và Unity](#3-build-launcher-và-unity)
4. [Triển khai AWS](#4-triển-khai-aws)
5. [SMTP và OTP](#5-smtp-và-otp)
6. [Scene và điều khiển](#6-scene-và-điều-khiển)
7. [Bảo mật và kiểm thử trước khi phát hành](#7-bảo-mật-và-kiểm-thử-trước-khi-phát-hành)

## 1. Kiến trúc

```text
WinForms Launcher ── TCP 9000 ── DanToPhong.Server ── SQL Server / RDS
        │                                  │
        └── khởi chạy Unity với token       └── email OTP qua Gmail SMTP

Unity ── Photon Cloud (phòng, đồng bộ xe, voice) ── Unity của người chơi khác
```

Giao thức TCP dùng frame 4-byte big-endian và JSON. Token xác thực được truyền từ Launcher sang game; không truyền lại mật khẩu người dùng. Phòng Photon và trạng thái phòng/lời mời của TCP có một số phần lưu trong RAM, vì vậy triển khai hiện tại chỉ dành cho một server instance.

## 2. Yêu cầu và chạy cục bộ

- Windows 10/11 và .NET SDK 10 cho Launcher/Server.
- Unity Editor đúng phiên bản ghi trong `ProjectSettings/ProjectVersion.txt` để sửa hoặc build game.
- SQL Server tương thích, hoặc endpoint RDS đã cấu hình truy cập từ máy Server.
- Tài khoản Photon PUN/Voice và App ID hợp lệ để chơi Multiplayer.

### Chạy TCP Server

Tạo connection string trong biến môi trường cục bộ hoặc secret store; không ghi mật khẩu DB vào source hay lệnh được commit. Ví dụ PowerShell dưới đây dùng placeholder cần thay bằng giá trị của môi trường riêng:

```powershell
$env:DANTOPHONG_DB_CONNECTION = 'Server=<SQL-HOST>,1433;Database=DanToPhongDB;User Id=<DB-USER>;Password=<DB-PASSWORD>;Encrypt=True;TrustServerCertificate=False;'
$env:DANTOPHONG_PORT = '9000'
dotnet run --project .\UI\DanToPhong.Server\DanToPhong.Server.csproj
```

Server từ chối khởi động nếu chưa có `DANTOPHONG_DB_CONNECTION`; database/schema được khởi tạo khi service khởi động. Không mở SQL Server/RDS trực tiếp ra Internet.
`DANTOPHONG_PORT` mặc định `9000`; giá trị cấu hình không phải số hoặc ngoài khoảng `1..65535` sẽ làm Server báo lỗi thay vì lặng lẽ dùng cổng khác. `DANTOPHONG_USE_TLS` mặc định `false` khi không đặt; nếu đặt, chỉ nhận `true` hoặc `false`.

### Chạy Launcher

```powershell
dotnet run --project .\UI\UI\UI.csproj
```

Client đọc `server.json` cạnh ứng dụng (không phân biệt hoa-thường) hoặc các biến `DANTOPHONG_SERVER_HOST`, `DANTOPHONG_SERVER_PORT`, `DANTOPHONG_USE_TLS`. Mẫu cấu hình ở [deploy/client/server.json.example](deploy/client/server.json.example). Thay host bằng endpoint đã triển khai; bật TLS ở client và server cùng lúc khi chứng chỉ đã cấu hình.
Nếu biến `DANTOPHONG_SERVER_PORT` được đặt, phải là số nguyên `1..65535`; giá trị sai sẽ báo lỗi cấu hình thay vì âm thầm dùng cổng trong JSON.

## 3. Build Launcher và Unity

### Launcher Release

```powershell
dotnet publish .\UI\UI\UI.csproj -c Release -r win-x64 --self-contained false -o D:\DanToPhong\LauncherRelease
Copy-Item .\deploy\client\server.json.example D:\DanToPhong\LauncherRelease\server.json
```

Điền `host` trong `server.json` bằng Elastic IP hoặc DNS của server đã triển khai. Bản framework-dependent cần .NET Desktop Runtime 10 trên máy bạn bè.

### Unity

- Scene khởi động là `boot` ở index 0.
- Các scene build: `menu`, `LobbyScene`, `SelectScene`, `ShopScene`, `game 1`, `game 2`, `game 3`, `game`.
- `game` là scene fallback mặc định cho map; phải giữ trong Build Settings.
- Launcher mở chế độ `single` hoặc multiplayer bằng `--user`, `--token`, `--host`, `--port`, `--tls`, `--mode`, và tùy chọn `--room`.
- Các scene bắt đầu bằng `game` khởi tạo `GameNetworkManager`; xe người chơi khác được điều khiển bằng dữ liệu Photon, không nhận input cục bộ.

Mọi scene nguồn và `EditorBuildSettings.asset` cần được commit để bản clone có đủ nội dung Unity. Kiểm tra Photon App IDs/region/version riêng trong Unity trước khi build; không đưa thông tin xác thực SMTP/DB vào game client.

## 4. Triển khai AWS

### Elastic IP cho EC2

AWS CLI cần được cấu hình quyền tối thiểu cho profile `dantophong`. Địa chỉ Elastic IP/public IPv4, EC2 và RDS có thể tính phí; không chạy lệnh thay đổi nếu chưa được chủ tài khoản duyệt. Trước tiên chỉ đọc trạng thái:

```powershell
$profile = 'dantophong'
$region = 'us-east-1'
$instance = 'i-05bff5cbf44d8f35d'
aws rds describe-db-instances --profile $profile --region $region --db-instance-identifier dantophong-db --query 'DBInstances[].{Id:DBInstanceIdentifier,Status:DBInstanceStatus,PubliclyAccessible:PubliclyAccessible}' --output table
aws ec2 describe-instances --profile $profile --region $region --instance-ids $instance --query 'Reservations[].Instances[].{Id:InstanceId,State:State.Name,PublicIp:PublicIpAddress}' --output table
aws ec2 describe-addresses --profile $profile --region $region --query 'Addresses[].{PublicIp:PublicIp,AllocationId:AllocationId,InstanceId:InstanceId,NetworkInterfaceId:NetworkInterfaceId,AssociationId:AssociationId,Tags:Tags}' --output json
```

Runbook idempotent để gắn EIP, start EC2/RDS và dừng an toàn có trong [báo cáo Chương 13](BAO_CAO_TRIEN_KHAI_VA_KIEM_THU.md#13-runbook-bậttắt-aws-test-internet-và-git). Nó không chuyển EIP đang gắn ENI/resource khác; nếu có EIP chưa nhận diện, chỉ cấp thêm sau khi xác minh resource cũ và nhập xác nhận chi phí riêng. Lần kiểm tra gần nhất thấy EC2 stopped và EIP hiện hữu gắn ENI có mô tả `RDSNetworkInterface`; không tự ý chuyển địa chỉ. RDS hiện `PubliclyAccessible=true`; kiểm tra Security Group trước khi bật, giữ SQL 1433 chỉ từ nguồn quản trị cần thiết và Security Group EC2. TCP 9000 tới EC2 cần TLS trước khi dùng credential thật.

### Tự khởi động và tự phục hồi service

Unit mẫu: [deploy/aws/dantophong.service](deploy/aws/dantophong.service). Mẫu biến môi trường không chứa credential thật: [deploy/aws/server.env.example](deploy/aws/server.env.example).

1. Publish Server cho Linux x64, cài .NET 10 runtime trên EC2 và chép output vào `/opt/dantophong`.
2. Tạo `/etc/dantophong/server.env` từ mẫu; điền DB, SMTP và certificate secret từ secret manager/SSM Parameter Store. Đặt quyền `root:root`, mode `600`; tuyệt đối không commit file này.
3. Cài unit vào `/etc/systemd/system/dantophong.service`, sau đó chạy:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now dantophong
sudo systemctl status dantophong
sudo journalctl -u dantophong -f
```

`WantedBy=multi-user.target` bật service theo boot; `Restart=always` và `RestartSec=5` khởi động lại khi process thoát. `enable` cần thực hiện một lần cho mỗi instance. RDS phải ở trạng thái available, cho phép ingress từ EC2 security group, và connection string phải nằm ngoài repo.

### TLS

Server hỗ trợ TLS qua `DANTOPHONG_USE_TLS`, `DANTOPHONG_CERT_PATH` và `DANTOPHONG_CERT_PASSWORD`; client hỗ trợ cờ `useTls` trong JSON hoặc `DANTOPHONG_USE_TLS`. Dùng chứng chỉ tin cậy phù hợp với hostname/IP mà client kết nối, bảo vệ private key, và bật TLS đồng bộ ở hai đầu. Không coi TCP không mã hóa là sẵn sàng cho Internet vì token/mật khẩu đi qua kết nối.

## 5. SMTP và OTP

Server hỗ trợ Gmail SMTP implicit TLS trên cổng 465 (mặc định) hoặc STARTTLS trên 587. Cấu hình trong môi trường Server: `DANTOPHONG_SMTP_HOST`, `DANTOPHONG_SMTP_PORT`, `DANTOPHONG_SMTP_USER`, `DANTOPHONG_SMTP_PASSWORD`. Dùng App Password của tài khoản gửi mail, không dùng mật khẩu đăng nhập Gmail; không đưa giá trị vào repo, log, báo cáo hay Unity.

OTP có thời hạn 10 phút, giới hạn thử sai và cooldown gửi lại. Nếu SMTP gửi lỗi, Server giải phóng reservation chưa có mã để lần thử kế tiếp có thể gửi thật; log chỉ để chẩn đoán phía Server. Get Code cần kiểm tra kết nối Launcher → TCP Server → Gmail SMTP, không chỉ kiểm tra TCP port từ máy client.

## 6. Scene và điều khiển

| Scene/luồng | Vai trò |
|---|---|
| `boot` | Entry scene; xác thực token nếu chạy từ Launcher, rồi định tuyến theo mode. |
| `menu` | Hub trong game và quay lại từ màn hình phụ. |
| `SelectScene` | Chọn xe/map; Solo bắt đầu từ đây. |
| `LobbyScene` | Tạo/tham gia phòng Photon; host đồng bộ scene bắt đầu. |
| `ShopScene` | Mua và trang bị vật phẩm qua Server. |
| `game`, `game 1`–`game 3` | Các đường đua; `game` là fallback mặc định. |

Xe dùng Rigidbody và xử lý input/vật lý ở máy chủ sở hữu; `SmoothNetworkTransform` gửi transform/tốc độ và nội suy xe từ xa. Có reset/hồi sinh khỏi nước, va chạm và log chẩn đoán. Các thông số vật lý, network jitter, reset, va chạm và round-trip scene cần được xác nhận bằng Unity build thật; việc đọc code không thay thế test hai client ngoài Internet.

Điều khiển mặc định: `W`/`↑` ga, `S`/`↓` phanh/lùi, `A`/`D` rẽ, `Space` nhảy, `Esc` tạm dừng/thoát trận.

## 7. Bảo mật và kiểm thử trước khi phát hành

- Không commit `.env`, `server.env`, private key/certificate, DB password, SMTP App Password, hoặc dữ liệu cá nhân. Dùng `.gitignore` và kiểm tra `git status --ignored`.
- TCP/9000 cần TLS trước khi dùng tài khoản thật qua Internet. RDS không public; chỉ EC2 được truy cập cổng DB.
- Photon App ID là cấu hình client, không phải secret; không nhúng DB/SMTP credential vào Unity hay Launcher.
- Mỗi clone cần .NET 10 Desktop Runtime, Launcher Release và `server.json` có endpoint public; Photon phải có cấu hình hợp lệ. Không cần người chơi cài SQL Server khi dùng service cloud.
- Chạy kiểm thử thủ công: đăng ký+OTP, OTP sai/hết hạn/dùng lại, login/logout và token, mở game bằng token, Solo qua chọn map/kết thúc/reset, hai máy tạo/vào phòng/rời phòng, scene sync và network jitter. Kiểm tra log `journalctl -u dantophong` khi lỗi.
- Trước khi phát hành ổn định, xác minh EC2 đang chạy và có EIP, RDS sẵn sàng, TLS hoạt động, SMTP gửi thật, Photon App ID/region, hai client Internet, và quyền Security Group tối thiểu.

**Lưu ý hiện trạng:** việc kiểm tra repo không tự khởi động EC2, cấp Elastic IP, thay đổi Security Group/RDS, hoặc gửi email kiểm thử. Cần thực hiện các bước AWS và test end-to-end nêu trên trước khi phát hành công khai.
