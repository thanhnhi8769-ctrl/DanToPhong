<h1 align="center">🛵 DÂN TỔ PHÓNG - MULTIPLAYER RACING GAME</h1>

**Dân Tổ Phóng** là tựa game đua xe scooter 3D tốc độ cao hỗ trợ cả hai chế độ **Chơi đơn (Solo)** và **Chơi mạng nhiều người (Multiplayer)** thời gian thực thông qua **Photon PUN 2**! Nhiệm vụ của bạn là làm chủ tay lái, thu thập các đồng xu checkpoint để duy trì quỹ thời gian, tránh rơi xuống vùng nước biển, vượt qua cổng chiến thắng để tích lũy Xu và mở khóa các vật phẩm độc đáo trong **Cửa hàng 3D**.

---

## 🌐 1. Chế Độ Chơi & Phòng Chờ Mã Phòng (Lobby & Room Code)

Game hỗ trợ linh hoạt **Chơi đơn (1 người)** và **Chơi mạng tối đa 4 người** kết nối trực tiếp qua Server khu vực **Asia** bằng **Mã phòng 5 ký tự**:

- **Tạo phòng & Cấp mã tự động (Create Room):**
  - Bấm **Create** để hệ thống tự sinh ngẫu nhiên **Mã phòng gồm 5 ký tự** viết hoa (loại bỏ các ký tự dễ nhìn nhầm như `O, 0, I, 1`).
  - Mã phòng được **tự động sao chép sẵn vào Clipboard** ngay khi tạo phòng, kèm nút **SAO CHÉP MÃ** tiện lợi ở góc dưới bên phải để gửi nhanh cho bạn bè.
- **Tham gia phòng bằng mã (Join Room):**
  - Nhập **Mã phòng 5 ký tự** (tự động chuyển thành chữ in hoa) hoặc **nhấp chuột phải trực tiếp vào ô nhập mã** để dán nhanh từ Clipboard, sau đó nhấn `Enter` hoặc bấm **Join**.
  - Hệ thống kiểm tra và thông báo chi tiết nếu sai mã phòng, phòng đã đầy (`4/4`) hoặc trận đấu đã bắt đầu (phòng tự khóa khi vào đua).
- **Sảnh chờ trực tuyến thông minh:**
  - Hiển thị thời gian thực **Mã phòng**, sĩ số (`x/4`), danh sách tên người chơi kèm nhãn `(Chủ phòng)` / `(Bạn)` và thông báo nổi bật mỗi khi có người **vào phòng** hoặc **rời phòng**.
  - **Chuyển quyền Chủ phòng tự động (Host Migration):** Nếu Chủ phòng thoát khỏi sảnh chờ, hệ thống tự động chuyển quyền Chủ phòng và nút **START GAME** cho người chơi kế tiếp.
  - Khi mọi người đã vào đủ, **Chủ phòng** bấm **START GAME** để khóa phòng và đồng bộ đưa toàn bộ thành viên vào đường đua.
- **Đồng bộ xuất phát:** Khi vào đường đua, xe của mọi người chơi được giữ cố định tại vạch xuất phát cho đến khi **tất cả thành viên trong phòng** cùng bấm **Bắt Đầu Đua** trên bảng hướng dẫn (ở chế độ Solo, game bắt đầu ngay sau khi bấm).
- **Chế độ Khán giả (Spectator Mode) khi bị loại:**
  - Trong chế độ nhiều người chơi, nếu hết giờ giữa chừng, xe của bạn sẽ biến mất khỏi đường đua và bảng **Eliminate** hiện lên cho phép bạn chọn **Ở lại xem** hoặc **Thoát**.
  - Khi ở lại xem, camera tự động đồng bộ đúng góc nhìn **Third-Person** của người chơi đang thi đấu và hỗ trợ chuyển đổi qua lại giữa các tay đua còn sống.
- **Phân định Thắng / Thua & Phần thưởng Xu:**
  - **Điều kiện thắng Multiplayer:** Là người đầu tiên chạm cổng **Checkwin**, hoặc là **người chơi cuối cùng còn sống sót** khi tất cả đối thủ đều đã bị loại. Người thắng nhận **+50 Xu**, người thua nhận **0 Xu** và hiển thị trên bảng **Result**.
  - **Chế độ Solo:** Vượt qua cổng **Checkwin** trước khi hết giờ để nhận **+10 Xu** và mở bảng chiến thắng tiếp tục chơi.

---

## 🛒 2. Hệ Thống Cửa Hàng 3D & Tủ Đồ Sưu Tập (3D Shop & Inventory)

Từ màn hình Menu chính, người chơi có thể truy cập **Cửa hàng (Shop)** để dùng số Xu tích lũy được mở khóa trang bị cá nhân hóa:

- **3 Danh mục vật phẩm (Tabs):**
  - **Scooter:** Các mẫu xe tay ga / scooter 3D đa dạng kiểu dáng.
  - **Helmet:** Các mẫu mũ bảo hiểm cá tính.
  - **Character:** Các nhân vật tay đua 3D.
- **Sân khấu trưng bày 3D xoay 360° (3D Showcase Stage):**
  - Hệ thống tự động chuẩn hóa kích thước, căn giữa tâm hình học (*Bounds*) và vô hiệu hóa vật lý thừa để trưng bày mô hình 3D xoay tròn 360° mượt mà ngay giữa màn hình.
  - Hỗ trợ chuyển qua lại giữa các vật phẩm (`Next` / `Previous`), xem tên, giá bán và mua trực tiếp bằng Xu (`BUY`) với thông báo trạng thái tức thì (`PURCHASED` / `NOT ENOUGH COINS` / `OWNED`).
- **Kệ trưng bày đồ đã sở hữu trực quan (Mini Owned Shelf):**
  - Ở góc trên bên trái màn hình Shop hiển thị **Kệ đồ 3D thu nhỏ (`OWNED ITEMS`)** phân theo 3 hàng (`SCOOTER`, `HELMET`, `CHAR`).
  - Toàn bộ vật phẩm bạn đã mở khóa được nhân bản thu nhỏ, tự động mở rộng khung kệ và xoay 3D trực tiếp để bạn theo dõi bộ sưu tập của mình.

---

## 🎮 3. Nút Điều Khiển (Controls)

### Khi ở Sảnh chờ (Lobby):

| Thao tác / Phím bấm | Chức năng |
| :--- | :--- |
| `Chuột phải` vào ô nhập mã | Dán nhanh mã phòng 5 ký tự từ Clipboard |
| `Enter` (trong ô nhập mã) | Tham gia phòng ngay lập tức (`Join Room`) |
| `ESC` | Rời phòng hiện tại hoặc quay về màn hình Menu chính |

### Khi đang đua:
Sau khi cuộc đua bắt đầu, chỉ cần nhấn phím tăng tốc lần đầu, xe sẽ **duy trì ga chạy về phía trước**. Bạn có thể sử dụng linh hoạt cụm phím **`WASD`** hoặc các phím **`Mũi tên`**:

| Phím bấm | Chức năng |
| :--- | :--- |
| `W` hoặc `↑` | **Khởi động & Tăng tốc (Boost):** Nhấn giữ để xe bứt tốc chạy nhanh hơn nữa |
| `S` hoặc `↓` | **Phanh / Lùi xe:** Hãm tốc độ khi vào cua gắt hoặc lùi xe khi bị vướng chướng ngại vật |
| `A` hoặc `←` | **Rẽ trái:** Bẻ lái xe sang bên trái |
| `D` hoặc `→` | **Rẽ phải:** Bẻ lái xe sang bên phải |
| `Space` (Dấu cách) | **Nhảy:** Nhảy qua các chướng ngại vật hoặc nhảy thoát khỏi vùng nước |
| `ESC` | **Tạm dừng / Thoát trận:** Mở bảng tùy chọn (`Resume` để tiếp tục hoặc `Quit` để rời phòng quay về Menu chính) |

### Khi ở chế độ Khán giả (Spectator Mode):

| Phím bấm / Nút | Chức năng |
| :--- | :--- |
| `E`, `→`, `Tab` hoặc nút `>` | Chuyển sang xem camera của tay đua tiếp theo |
| `Q`, `←` hoặc nút `<` | Quay lại xem camera của tay đua trước đó |
| `ESC` | Mở / đóng bảng tùy chọn **Ở lại xem** hoặc **Thoát trận** |

---

## 🗺️ 4. Hệ Thống Bản Đồ Thu Nhỏ (Minimap GPS)

- **Góc nhìn trực diện từ trên xuống:** Camera Minimap (Orthographic) bám sát theo xe của người chơi, giúp quan sát rõ toàn cảnh cung đường và các khúc cua phía trước.
- **Mũi tên định hướng GPS phân biệt người chơi:**
  - **Mũi tên Xanh lá (`Green`):** Vị trí và hướng di chuyển hiện tại của **xe bạn**.
  - **Mũi tên Đỏ (`Red`):** Vị trí và hướng di chuyển trực tiếp của **xe đối thủ** trên đường đua.

---

## ⏱️ 5. Luật Chơi, Checkpoint & Vùng Nước Phạt (Rules)

- **Thời gian ban đầu:** Mỗi tay đua có sẵn **30 giây** đếm ngược khi bắt đầu lượt chạy.
- **Đồng xu Checkpoint xoay tròn:** Trên đường đua xuất hiện các **Đồng xu (Coins)** tự động xoay tròn tại chỗ. Mỗi khi ăn được 1 đồng xu checkpoint, bạn được **cộng thêm +5 giây** vào quỹ thời gian kèm thông báo và hiệu ứng âm thanh.
- **Vùng nước biển (`Sea_Plane`) trừ thời gian:** Nếu lái xe trượt hoặc nhảy rơi xuống vùng nước biển bao quanh đường đua (hoặc hạ thấp dưới mực nước `Y <= -0.3`), hệ thống sẽ hiện cảnh báo đỏ và **trừ 5 giây** mỗi lần rơi nước (không trừ xu) cho đến khi bạn nhảy thoát lên bờ.
- **Cổng Checkwin:** Vượt qua các chướng ngại vật, thu thập checkpoint và cán đích tại **Cổng Checkwin** (biển hiệu **Billboard Finish**) sớm nhất để giành chiến thắng.
- **Điều kiện bị loại / thua cuộc:** Thời gian đếm ngược của bạn về `0` trước khi chạm đích, hoặc có đối thủ khác về đích trước bạn.

---

## 🔊 6. Hệ Thống Âm Thanh & Giao Diện Trực Quan (Audio & UI)

- **Nhạc nền xuyên suốt:** Nhạc nền tự động phát từ màn hình **Menu** sang **Phòng chờ (Lobby)** và chuyển sang nhạc đường đua sôi động khi vào trận (tự động tạm dừng khi mở bảng `ESC` ở chế độ Solo và dừng khi kết thúc ván).
- **Hiệu ứng âm thanh (SFX):** Tích hợp âm thanh phản hồi riêng khi ăn đồng xu Checkpoint, Chiến thắng (Win) và Bị loại / Thua cuộc (Game Over).
- **Hiển thị Xu & Bảng tin sự kiện:**
  - Luôn hiển thị **tổng số Xu hiện có** ở góc trên bên trái màn hình và tự động đồng bộ vào các bảng kết quả cũng như Cửa hàng.
  - Bảng tin thông báo tự động gom nhóm và mờ dần khi có người chơi bị loại, thoát phòng, nhận thưởng thời gian hoặc rơi xuống nước.

---

## 💡 7. Mẹo Đua Xe (Pro Tips)

- **Vào phòng cực nhanh:** Chủ phòng chỉ cần bấm **Create** là mã 5 ký tự đã nằm sẵn trong Clipboard để `Ctrl + V` gửi cho bạn bè; người vào phòng chỉ cần **chuột phải** vào ô nhập mã là dán ngay.
- **Theo dõi mũi tên Đỏ trên Minimap:** Quan sát khoảng cách và hướng rẽ của đối thủ để chủ động bứt tốc ở các đoạn đường thẳng.
- **Căn thời gian nhảy (`Space`):** Tận dụng phím nhảy (có hỗ trợ *Coyote Time* & *Jump Buffer*) để vượt chướng ngại vật, cắt góc lề đường hoặc nhảy thoát nhanh nếu chẳng may rơi xuống nước `Sea_Plane`.
- **Tăng tốc thông minh (`W` / `↑`):** Giữ tăng tốc trên các đoạn đường thẳng dài và nhả phím trước khi vào cua gấp để tránh văng khỏi đường đua xuống biển.
- **Cày Xu mở khóa Shop:** Chơi chế độ Multiplayer để nhận **+50 Xu/trận thắng** giúp mở khóa nhanh toàn bộ bộ sưu tập Scooter, Helmet và Character trên kệ trưng bày 3D.

---

## 🛠️ 8. Công Nghệ Sử Dụng

- **Game Engine:** Unity 6 (Universal Render Pipeline - URP)
- **Networking:** Photon PUN 2 (Photon Unity Networking - Asia Region, Room Code & Host Migration)
- **Camera System:** Unity Cinemachine, Networked Third-Person Spectator Camera & Orthographic Minimap Camera
- **UI, 3D Showcase & Audio:** TextMeshPro (TMP), Dynamic 3D Bounds Normalization & Multi-channel AudioSource

---

## 📦 9. Tải Bản Build Game (BuildGame.zip)

👉 [Tải xuống BuildGame.zip tại đây](https://drive.google.com/drive/folders/19mJVw_RUIEru4CHCZbzZtJtPiwsEHSFR?usp=sharing)

---

<div align="center">

### ĐỒ ÁN MÔN HỌC - LẬP TRÌNH MẠNG CĂN BẢN
**NHÓM 4**

</div>
