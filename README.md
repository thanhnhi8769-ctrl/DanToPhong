<h1 align="center">🛵 DÂN TỔ PHÓNG - MULTIPLAYER RACING GAME</h1>

**Dân Tổ Phóng** là game đua xe scooter 3D hỗ trợ **chơi đơn** và **chơi mạng tối đa 4 người** (Photon PUN 2 + Voice chat). Người chơi đăng nhập bằng **Launcher (WinForms)**, dữ liệu tài khoản / xu / xe / avatar lưu trên **SQL Server** qua **Server TCP** của nhóm, nên đăng nhập máy nào cũng thấy đúng dữ liệu của mình.

---

## 🧩 1. Kiến trúc hệ thống

```
┌────────────────────┐   TCP :9000    ┌──────────────────────┐   SQL    ┌──────────────┐
│ Launcher (WinForms)│ ─────────────► │  DanToPhong.Server   │ ───────► │ SQL Server   │
│  đăng nhập, hồ sơ, │                │  (C#, console)       │          │ DanToPhongDB │
│  phòng, xếp hạng   │                └──────────▲───────────┘          └──────────────┘
└─────────┬──────────┘                           │ TCP :9000 (cùng giao thức, xác thực bằng token)
          │ mở game kèm --token                  │
          ▼                                      │
┌────────────────────┐   Photon cloud (internet) │
│ Game Unity (.exe)  │ ──────────────────────────┘
│  xe, shop, đua mạng│ ◄───────────► người chơi khác
└────────────────────┘
```

- **Launcher** đăng nhập → nhận `token` → mở game bằng dòng lệnh `--user --token --host --port --mode`.
- **Game** dùng token để kết nối lại với Server (không cần nhập mật khẩu lần nữa) và đọc/ghi **xu, xe, map, avatar, danh sách phòng, lời mời** trên Server.
- **Photon** chỉ lo phần đua mạng (phòng, vị trí xe, chat, voice), cần có internet.

---

## ✅ 2. Yêu cầu cài đặt

| Thành phần | Phiên bản |
| :--- | :--- |
| Windows | 10 / 11 |
| Visual Studio | 2022 (workload **.NET desktop development**) |
| .NET SDK | bản ghi trong file `.csproj` (từ .NET 6 trở lên, khuyến nghị .NET 8) |
| SQL Server + SSMS | 2019 / 2022 (Express cũng được), bật **TCP/IP** cổng `1433` |
| Unity (nếu muốn mở / build game) | **Unity 6.3 LTS (6000.3.28f1)** |
| Photon | tài khoản miễn phí tại [dashboard.photonengine.com](https://dashboard.photonengine.com) |

> Chỉ muốn **chơi thử** (không sửa code)? Chỉ cần SQL Server + .NET SDK + **BuildGame.zip** (mục 8), không cần cài Unity.

---

## 🚀 3. Chạy nhanh (4 bước)

### Bước 1 - Cài SQL Server (bỏ qua nếu máy đã có)

Chọn **một** trong hai cách:

**Cách A: SQL Server 2022 Developer (miễn phí, khuyên dùng)**

1. Tải **SQL Server 2022 Developer** tại <https://www.microsoft.com/sql-server/sql-server-downloads>, cài kiểu **Basic**.
2. (Tùy chọn) Cài **SSMS** để xem dữ liệu: <https://aka.ms/ssmsfullsetup>.
3. Bật đăng nhập `sa`: mở SSMS, kết nối `localhost` bằng *Windows Authentication* → chuột phải server → *Properties* → *Security* → chọn **SQL Server and Windows Authentication mode**. Rồi vào *Security → Logins → `sa` → Properties*: đặt **Password** (tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt) và ở tab *Status* chọn **Login: Enabled**.
4. Bật TCP cổng 1433: mở **SQL Server Configuration Manager** → *SQL Server Network Configuration* → *Protocols for MSSQLSERVER* → **TCP/IP = Enabled** → *Properties* → tab *IP Addresses* → kéo xuống mục **IPAll**: để trống *TCP Dynamic Ports*, đặt **TCP Port = 1433**.
5. **Restart** dịch vụ SQL Server (chuột phải server trong SSMS → *Restart*, hoặc trong Configuration Manager → *SQL Server Services*).

**Cách B: Docker (nếu đã cài Docker Desktop, nhanh nhất)**

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=DanToPhong@123" -p 1433:1433 --name sql-dtp -d mcr.microsoft.com/mssql/server:2022-latest
```

Mật khẩu `sa` ở lệnh trên là `DanToPhong@123`. Dùng đúng mật khẩu này (hoặc mật khẩu bạn tự đặt) ở Bước 2.

> ✅ **Không cần tạo database hay bảng thủ công.** Khi chạy lần đầu, `DanToPhong.Server` tự tạo database `DanToPhongDB` cùng các bảng `NguoiDung`, `VatPham`, `DanhGia`. Muốn tạo tay thì chạy file `SQL/00_setup_full.sql` trong SSMS. Tài khoản mới luôn bắt đầu với **100 xu**.

### Bước 2 - Chạy Server

1. Mở `Program.cs` của project **`DanToPhong.Server`**, sửa mật khẩu `sa` ở `ConnectionString`:
   ```csharp
   "Server=localhost,1433;Database=DanToPhongDB;User Id=sa;Password=<MẬT_KHẨU_SA_CỦA_BẠN>;TrustServerCertificate=True;"
   ```
2. Mở terminal tại thư mục `DanToPhong.Server` và chạy:
   ```bash
   dotnet run
   ```
3. Đợi đủ các dòng sau rồi **để cửa sổ đó mở suốt**:
   ```
   [DB] Database setup OK.
   [DB] Connected to DanToPhongDB successfully.
   [TCP] Server listening on port 9000...
   ```

### Bước 3 - Đặt bản build game vào đúng chỗ

Launcher tìm file game theo thứ tự sau (lấy chỗ đầu tiên có file):

1. `D:\DanToPhong\BuildGame36\DanToPhong.exe` (hoặc `ScooterRacing.exe`)
2. `<thư mục chạy Launcher>\Game\DanToPhong.exe` (hoặc `ScooterRacing.exe`)

Tải **BuildGame.zip** (mục 8), giải nén **toàn bộ** vào một trong hai chỗ trên (giữ nguyên cả thư mục `*_Data`, `UnityPlayer.dll`...).

> Muốn đổi đường dẫn mặc định: sửa hằng `Build36Folder` trong `UI/Services/GameLauncherService.cs`.

### Bước 4 - Mở Launcher

1. Mở solution trong thư mục `UI` bằng Visual Studio, đặt **UI** làm Startup Project, bấm **F5**.
2. **Register** để tạo tài khoản, rồi **Login**.
3. Trong Launcher: **Single Player** (chơi đơn), **Multiplayer** (vào sảnh), tab **Open Rooms** (xem và vào phòng đang mở), **Create Room** (tự tạo phòng).

> ⚠️ Luôn bật **Server trước** rồi mới mở Launcher. Nếu tắt Server giữa chừng, đăng nhập lại.

---

## 🌍 4. Chơi nhiều máy (LAN / Internet)

Chỉ **một máy** chạy SQL Server + `DanToPhong.Server` (gọi là *máy chủ*). Các máy còn lại chỉ chạy Launcher + game.

1. **Máy chủ:** mở firewall cho cổng `9000` (PowerShell quyền Admin):
   ```powershell
   New-NetFirewallRule -DisplayName "DanToPhong Server" -Direction Inbound -Protocol TCP -LocalPort 9000 -Action Allow
   ```
2. Lấy IP máy chủ (`ipconfig` → IPv4, ví dụ `192.168.1.20`). Khác mạng LAN thì dùng VPN (Radmin / ZeroTier / Tailscale) hoặc mở port.
3. **Máy khách:** trong project `UI`, tìm chỗ tạo `new NetworkClientService()` (thường ở `Program.cs`) và đổi thành:
   ```csharp
   new NetworkClientService("192.168.1.20", 9000)
   ```
   Launcher sẽ tự truyền `--host`/`--port` này cho game.
4. Tất cả máy cần **internet** để dùng Photon.

**Test hai tài khoản trên cùng một máy:** mở 2 cửa sổ Launcher, đăng nhập 2 tài khoản khác nhau.

---

## 🛠️ 5. Dành cho dev: mở / build game Unity

1. Mở thư mục project Unity bằng **Unity Hub** (đúng phiên bản `6000.3.28f1`).
2. **Photon:** nếu repo không kèm `PhotonServerSettings`, vào *Window → Photon Unity Networking → Highlight Server Settings* và dán **App Id PUN** và **App Id Voice** (tạo trên dashboard Photon). Game cố định vùng **Asia**.
3. **Build Settings** (*File → Build Profiles → Scene List*): scene **`Boot` phải ở index 0**, sau đó `menu`, `LobbyScene`, `SelectScene`, `ShopScene`, `game`, `game 1`, `game 2`, `game 3`, `ResultP...`. Mọi scene đua phải được **tích chọn**, vì thiếu scene sẽ kẹt ở "Loading track...".
4. **Build** ra thư mục `D:\DanToPhong\BuildGame36` (hoặc `Game\` cạnh Launcher), file `.exe` tên `DanToPhong.exe`.
5. Bấm Play trong Editor **không qua Launcher** thì game chạy chế độ **offline** (dữ liệu lưu `PlayerPrefs`), không nối Server. Muốn thử đầy đủ hãy **build rồi mở bằng Launcher**.

Tham số dòng lệnh game nhận từ Launcher:

| Tham số | Ý nghĩa |
| :--- | :--- |
| `--user` | tên đăng nhập |
| `--token` | token xác thực do Server cấp (hiệu lực 6 giờ) |
| `--host` / `--port` | địa chỉ Server (mặc định `127.0.0.1:9000`) |
| `--mode` | `single`, `multiplayer`, `create`, `room`, `shop`, `menu` |
| `--room` | mã phòng 5 ký tự (khi `--mode room`) |

---

## 🧭 6. Luồng màn hình & tính năng

```
Launcher ─► Game
              │
              ├─ single ──► Chọn xe ──► Đường đua
              ├─ multiplayer / create / room ──► Lobby ──► Chọn xe ──► Lobby ──START──► Đường đua
              └─ menu ──► Play / Shop / Leaderboard / Quit
```

- **Menu** là trung tâm: Shop, Select, Lobby thoát (`ESC` / Back) đều quay về Menu hoặc Lobby; chỉ nút **QUIT** ở Menu mới thoát game về Launcher.
- **Thẻ người chơi** ở Menu: avatar, tên, `@username • ID`, xu, xe đang dùng.
- **Avatar** lưu trong database, đổi ở tab **Settings** của Launcher (ảnh tự cắt vuông thu nhỏ 128x128).
- **Racer ID** là mã 6 số **duy nhất** (xem ở tab Racer, bấm để copy) dùng để **mời bạn bè** vào phòng.
- **Mời bạn:** trong phòng chờ, nhập **ID hoặc username** của bạn → **INVITE**. Người kia nhận lời mời trong vài giây (ở Launcher hoặc trong game).
- **Phòng:** chủ phòng báo trạng thái lên Server, tab **Open Rooms** của Launcher hiện **mọi** phòng đang mở (có thanh cuộn). Bấm **Join** là game tự vào đúng phòng.
- **Leaderboard** (top 20 theo xu): hạng, ID, tên người chơi. Mở từ Launcher hoặc Menu game.
- **Đánh giá game:** mỗi tài khoản 1 lượt, lưu trong database.
- **Xu:** thắng multiplayer **+50**, solo về đích **+10**; mua xe trong Shop (25 mẫu). Mọi thay đổi đồng bộ lên Server.

### Điều khiển khi đua

| Phím | Chức năng |
| :--- | :--- |
| `W` / `↑` | Tăng tốc / Boost |
| `S` / `↓` | Phanh / lùi |
| `A` `D` / `←` `→` | Rẽ trái / phải |
| `Space` | Nhảy |
| `MIC`, `CHAT` + `Enter` | Voice chat, text chat |
| `ESC` | Tạm dừng / thoát trận |

Khán giả (sau khi bị loại): `E`/`→`/`Tab` xem tay đua kế, `Q`/`←` xem tay đua trước.

### Luật chơi

- Bắt đầu với **30 giây**, mỗi đồng xu checkpoint **+5 giây**.
- Rơi khỏi đường (xuống nước) → hồi sinh về vạch xuất phát, đồng hồ vẫn chạy.
- Hết giờ hoặc có người về đích trước → bị loại (vào chế độ khán giả nếu chơi mạng).
- Về đích ở cổng **Checkwin** để thắng.

---

## 🧯 7. Xử lý lỗi thường gặp

| Triệu chứng | Cách xử lý |
| :--- | :--- |
| `Cannot connect to server` / không đăng nhập được | Chưa bật Server, sai `host`, hoặc firewall chặn cổng `9000` |
| Server: `Cannot connect to DanToPhongDB` / `Login failed for user 'sa'` | Sai mật khẩu `sa` trong `ConnectionString`, chưa bật SQL Authentication, hoặc chưa Restart SQL Server |
| `Invalid object name 'dbo.NguoiDung'` trong SSMS | Đang chọn nhầm database (`master`). Chọn `DanToPhongDB`, hoặc chạy Server một lần để nó tự tạo bảng |
| Server: `A network-related or instance-specific error` / `error: 40` / `error: 26` | SQL Server chưa chạy, chưa bật **TCP/IP** hoặc chưa đặt cổng **1433** (Bước 1, Cách A, mục 4). Cài **Express** mặc định là instance `SQLEXPRESS` không nghe cổng 1433: hãy làm mục 4 hoặc dùng Cách B (Docker) |
| `Không tìm thấy file game` khi bấm Play | Chưa đặt build game đúng chỗ (mục 3, bước 3) |
| Game mở rồi tự đóng | Không kết nối / xác thực được Server (xem Console của Server, token hết hạn thì đăng nhập lại Launcher) |
| Kẹt ở "Loading track..." khi chơi mạng | Scene đua chưa có trong Build Settings, hoặc 2 máy dùng 2 bản build khác nhau. Build lại và đưa **cùng một bản** cho mọi người |
| Không tạo / vào được phòng | Máy không có internet hoặc Photon chưa có App Id |
| `Send failed. Check SMTP/App Password` khi lấy mã xác nhận | Gửi email xác nhận cần cấu hình SMTP (ví dụ Gmail **App Password**) trong `EmailVerificationService` của project `UI` |
| Xu / xe trong game lệch so với Launcher | Đang chạy game ngoài Launcher (offline) hoặc dùng bản build cũ. Build lại và mở bằng Launcher |

---

## 📦 8. Tải bản build game

👉 [Tải xuống BuildGame.zip tại đây](https://drive.google.com/drive/folders/19mJVw_RUIEru4CHCZbzZtJtPiwsEHSFR?usp=sharing)

---

## 🧰 9. Công nghệ sử dụng

- **Game:** Unity 6 (URP), Cinemachine, TextMeshPro, Photon PUN 2 + Photon Voice 2 (vùng Asia)
- **Launcher:** C# Windows Forms (.NET)
- **Server:** C# console, TCP socket với giao thức riêng (`MessageFramer`: 4 byte độ dài + JSON, cổng `9000`), xác thực bằng token
- **Cơ sở dữ liệu:** Microsoft SQL Server (`DanToPhongDB`): mật khẩu băm PBKDF2-SHA256 + salt

---

<div align="center">

### ĐỒ ÁN MÔN HỌC - LẬP TRÌNH MẠNG CĂN BẢN
**NHÓM 4**

</div>
