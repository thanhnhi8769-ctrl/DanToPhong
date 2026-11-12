<h1 align="center">🛵 DÂN TỔ PHÓNG - MULTIPLAYER RACING GAME</h1>

**Dân Tổ Phóng** là tựa game đua xe scooter 3D tốc độ cao hỗ trợ chế độ **Chơi mạng nhiều người (Multiplayer)** thời gian thực thông qua **Photon PUN 2**! Nhiệm vụ của bạn là làm chủ tay lái, thu thập các đồng xu checkpoint để duy trì quỹ thời gian, tránh rơi xuống vùng nước biển và vượt qua cổng chiến thắng trước đối thủ cũng như trước khi đồng hồ đếm về `0`.

---

## 🌐 1. Chế Độ Chơi Mạng & Phòng Chờ (Multiplayer & Lobby)

Game hỗ trợ tối đa **4 người chơi** cùng thi đấu trực tiếp trong một phòng:

- **Tạo phòng (Create Room):** Nhập tên phòng bất kỳ và bấm **Create** để tạo phòng mới và trở thành **Chủ phòng (Master Client)**.
- **Tham gia phòng (Join Room):** Nhập đúng tên phòng của bạn bè và bấm **Join** để vào phòng chờ.
- **Sảnh chờ trực tuyến:**
  - Hiển thị thời gian thực số lượng người chơi trong phòng (`x/4 người`) và thông báo mỗi khi có người chơi **vào phòng** hoặc **rời phòng**.
  - Khi tất cả đã sẵn sàng, **Chủ phòng** bấm **START GAME** để đồng bộ đưa toàn bộ người chơi vào đường đua.
- **Đồng bộ xuất phát:** Khi vào đường đua, xe của mọi người chơi sẽ được giữ cố định tại vạch xuất phát cho đến khi **tất cả thành viên trong phòng** cùng bấm **Bắt Đầu Đua** trên bảng hướng dẫn.
- **Phân định Thắng / Thua thời gian thực:** Người đầu tiên chạm cổng **Checkwin** sẽ giành chiến thắng ngay lập tức, đồng thời hệ thống tự động thông báo kết thúc lượt đua cho các đối thủ còn lại.

---

## 🎮 2. Nút Điều Khiển (Controls)

Sau khi cuộc đua bắt đầu, chỉ cần nhấn phím tăng tốc lần đầu, xe sẽ **duy trì ga chạy về phía trước**. Bạn có thể sử dụng linh hoạt cụm phím **`WASD`** hoặc các phím **`Mũi tên`**:

| Phím bấm | Chức năng |
| :--- | :--- |
| `W` hoặc `↑` | **Khởi động & Tăng tốc (Boost):** Nhấn giữ để xe bứt tốc chạy nhanh hơn nữa |
| `S` hoặc `↓` | **Phanh / Lùi xe:** Hãm tốc độ khi vào cua gắt hoặc lùi xe khi bị vướng chướng ngại vật |
| `A` hoặc `←` | **Rẽ trái:** Bẻ lái xe sang bên trái |
| `D` hoặc `→` | **Rẽ phải:** Bẻ lái xe sang bên phải |
| `Space` (Dấu cách) | **Nhảy:** Nhảy qua các chướng ngại vật hoặc nhảy thoát khỏi vùng nước |
| `ESC` | **Tạm dừng / Thoát trận:** Mở bảng tùy chọn (`Resume` để tiếp tục hoặc `Quit` để rời phòng quay về màn hình Menu chính) |

---

## 🗺️ 3. Hệ Thống Bản Đồ Thu Nhỏ (Minimap GPS)

- **Góc nhìn trực diện từ trên xuống:** Camera Minimap (Orthographic) bám sát theo xe của người chơi, giúp quan sát rõ toàn cảnh cung đường và các khúc cua phía trước.
- **Mũi tên định hướng GPS phân biệt người chơi:**
  - **Mũi tên Xanh lá (`Green`):** Vị trí và hướng di chuyển hiện tại của **xe bạn**.
  - **Mũi tên Đỏ (`Red`):** Vị trí và hướng di chuyển trực tiếp của **xe đối thủ** trên đường đua.

---

## ⏱️ 4. Luật Chơi, Checkpoint & Vùng Nước Phạt (Rules)

- **Thời gian ban đầu:** Mỗi tay đua có sẵn **30 giây** đếm ngược khi bắt đầu lượt chạy.
- **Đồng xu Checkpoint xoay tròn:** Trên đường đua xuất hiện các **Đồng xu (Coins)** tự động xoay tròn tại chỗ. Mỗi khi ăn được 1 đồng xu checkpoint, bạn sẽ được **cộng thêm +5 giây** vào quỹ thời gian kèm thông báo và hiệu ứng âm thanh.
- **Vùng nước biển (`Sea_Plane`) trừ thời gian:** Nếu lái xe trượt hoặc nhảy rơi xuống vùng nước biển bao quanh đường đua, hệ thống sẽ hiện cảnh báo và **trừ thêm 1 giây mỗi giây** tương ứng với thời gian xe ở dưới nước cho đến khi bạn nhảy thoát lên bờ.
- **Cổng Checkwin:** Vượt qua các chướng ngại vật, thu thập checkpoint và cán đích tại **Cổng Checkwin** (biển hiệu **Billboard Finish**) sớm nhất để giành chiến thắng.
- **Điều kiện thua cuộc:** Thời gian đếm ngược của bạn về `0` trước khi chạm đích, hoặc có đối thủ khác về đích trước bạn.

---

## 🔊 5. Hệ Thống Âm Thanh & Giao Diện Trực Quan (Audio & UI)

- **Nhạc nền xuyên suốt:** Nhạc nền tự động phát từ màn hình **Menu** sang **Phòng chờ (Lobby)** và chuyển sang nhạc đường đua sôi động khi vào trận (tự động tạm dừng khi mở bảng `ESC` và dừng khi kết thúc ván).
- **Hiệu ứng âm thanh (SFX):** Tích hợp âm thanh phản hồi riêng khi ăn đồng xu Checkpoint, Chiến thắng (Win) và Thua cuộc (Game Over).
- **Thông báo sự kiện nổi bật:** Hiển thị cảnh báo chữ trắng viền đen sắc nét giữa màn hình khi nhận thưởng thời gian, rơi xuống nước hoặc có người chơi chạm đích / thoát phòng.

---

## 💡 6. Mẹo Đua Xe (Pro Tips)

- **Theo dõi mũi tên Đỏ trên Minimap:** Quan sát khoảng cách và hướng rẽ của đối thủ để chủ động bứt tốc ở các đoạn đường thẳng.
- **Căn thời gian nhảy (`Space`):** Tận dụng phím nhảy để vượt qua chướng ngại vật, nhảy cắt góc lề đường hoặc nhảy thoát nhanh nếu chẳng may rơi xuống nước `Sea_Plane`.
- **Tăng tốc thông minh (`W` / `↑`):** Giữ tăng tốc trên các đoạn đường thẳng dài và nhả phím trước khi vào cua gấp để tránh văng khỏi đường đua xuống biển.
- **Xử lý khi bị kẹt:** Nếu xe va chạm vào mép đường hoặc vật cản, giữ phím `S` (hoặc `↓`) để lùi xe ra nhanh chóng.

---

## 🛠️ 7. Công Nghệ Sử Dụng

- **Game Engine:** Unity 6 (Universal Render Pipeline - URP)
- **Networking:** Photon PUN 2 (Photon Unity Networking)
- **Camera System:** Unity Cinemachine & Orthographic Minimap Camera
- **UI & Audio:** TextMeshPro (TMP) & Multi-channel AudioSource

---

## 📦 8. Tải Bản Build Game (BuildGame.zip)

👉 [Tải xuống BuildGame.zip tại đây](https://drive.google.com/drive/folders/19mJVw_RUIEru4CHCZbzZtJtPiwsEHSFR?usp=sharing)

---

<div align="center">

### ĐỒ ÁN MÔN HỌC - LẬP TRÌNH MẠNG CĂN BẢN
**NHÓM 4**

</div>
