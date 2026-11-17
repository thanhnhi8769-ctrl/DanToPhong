<h1 align="center">🛵 DÂN TỔ PHÓNG - MULTIPLAYER RACING GAME</h1>

**Dân Tổ Phóng** là tựa game đua xe scooter 3D tốc độ cao hỗ trợ cả hai chế độ **Chơi đơn (Solo)** và **Chơi mạng nhiều người (Multiplayer)** thời gian thực thông qua **Photon PUN 2**! Nhiệm vụ của bạn là làm chủ tay lái, thu thập các đồng xu checkpoint để duy trì quỹ thời gian, giữ vững tay lái không để rơi khỏi đường đua, giao tiếp cùng bạn bè qua **Voice Chat (Mic) & Text Chat**, vượt qua cổng chiến thắng để tích lũy Xu và mở khóa các vật phẩm độc đáo trong **Cửa hàng 3D**.

---

## 🧭 1. Luồng Màn Hình (Game Flow)

```
Menu ──Play──► Lobby ──Tạo / Vào phòng──► SELECT VEHICLE ──► Select ──Xác nhận──► Lobby ──START GAME──► Đường đua
  │                                                                                                       
  └──► Shop (mua & trang bị vật phẩm)
```

1. **Menu:** Bấm **Play** để vào thẳng **Sảnh chờ (Lobby)**, hoặc vào **Shop** để mua đồ.
2. **Lobby:** Tạo phòng hoặc nhập mã để vào phòng.
3. **Select Vehicle:** Sau khi đã ở trong phòng, bấm nút **SELECT VEHICLE** để sang màn hình chọn xe (và chọn map nếu bạn là Chủ phòng).
4. **Xác nhận:** Quay lại Lobby, **vẫn giữ nguyên phòng** và danh sách người chơi.
5. **START GAME:** Chủ phòng bấm để cả phòng cùng vào đường đua.

> 💡 Mỗi người chơi **chọn xe cho riêng mình**, và xe nào được chọn thì **đúng xe đó xuất hiện** trên đường đua (kể cả trong mắt những người chơi khác).

---

## 🌐 2. Chế Độ Chơi, Chọn Xe & Phòng Chờ (Lobby & Room Code)

Game hỗ trợ linh hoạt **Chơi đơn (1 người)** và **Chơi mạng tối đa 4 người** kết nối trực tiếp qua Server khu vực **Asia** bằng **Mã phòng 5 ký tự**:

- **Tạo phòng & Cấp mã tự động (Create Room):**
  - Bấm **Create** để hệ thống tự sinh ngẫu nhiên **Mã phòng gồm 5 ký tự** viết hoa (loại bỏ các ký tự dễ nhìn nhầm như `O, 0, I, 1`).
  - Mã phòng được **tự động sao chép sẵn vào Clipboard** ngay khi tạo phòng, kèm nút **SAO CHÉP MÃ** ở góc dưới bên phải để gửi nhanh cho bạn bè.
- **Tham gia phòng bằng mã (Join Room):**
  - Nhập **Mã phòng 5 ký tự** (tự động chuyển thành chữ in hoa) hoặc **nhấp chuột phải trực tiếp vào ô nhập mã** để dán nhanh từ Clipboard, sau đó nhấn `Enter` hoặc bấm **Join**.
  - Hệ thống thông báo chi tiết nếu sai mã phòng, phòng đã đầy (`4/4`) hoặc trận đấu đã bắt đầu (phòng tự khóa khi vào đua).
- **Chọn xe & bản đồ theo vai trò (nút `SELECT VEHICLE`):**

  | Vai trò | Được chọn | Giao diện |
  | :--- | :--- | :--- |
  | 👑 **Chủ phòng** | **Mẫu xe Scooter** + **Bản đồ (Map)** | Hộp kính 3D xoay 360° kèm khung xem trước ảnh Map |
  | 🙋 **Người chơi thường** | **Mẫu xe Scooter** | Hộp kính 3D được **phóng to** và đặt giữa màn hình, ghi chú *"Map do Chủ Phòng chọn"* |

  - Giao diện tự **điều chỉnh kích thước** tùy theo số vật được hiển thị: 2 vật (xe + map) thì hộp xe nhỏ gọn, 1 vật (chỉ xe) thì hộp xe lớn hơn.
  - Map do Chủ phòng chọn được lưu vào **Room Properties**, còn xe của từng người được lưu vào **Player Properties**, đồng bộ tự động cho toàn bộ phòng.
  - Mũ bảo hiểm (Helmet) và Nhân vật (Character) lấy theo **trang bị đang dùng trong Shop**.
- **Sảnh chờ trực tuyến thông minh:**
  - Hiển thị thời gian thực **Mã phòng**, sĩ số (`x/4`), danh sách tên người chơi kèm nhãn `(Chủ phòng)` / `(Bạn)` và thông báo nổi bật mỗi khi có người **vào phòng** hoặc **rời phòng**.
  - **Chuyển quyền Chủ phòng tự động (Host Migration):** Nếu Chủ phòng thoát, hệ thống tự động chuyển quyền Chủ phòng và nút **START GAME** cho người chơi kế tiếp.
  - Khi mọi người đã vào đủ, **Chủ phòng** bấm **START GAME** để khóa phòng và đồng bộ đưa toàn bộ thành viên vào đúng bản đồ đã chọn.
- **Đồng bộ xuất phát:** Khi vào đường đua, xe của mọi người chơi được giữ cố định tại vạch xuất phát cho đến khi **tất cả thành viên trong phòng** cùng bấm **Bắt Đầu Đua** trên bảng hướng dẫn (ở chế độ Solo, game bắt đầu ngay sau khi bấm).
- **Chế độ Khán giả (Spectator Mode) khi bị loại:**
  - Trong chế độ nhiều người chơi, nếu hết giờ giữa chừng, xe của bạn sẽ biến mất khỏi đường đua và bảng **Eliminate** hiện lên cho phép bạn chọn **Ở lại xem** hoặc **Thoát**.
  - Khi ở lại xem, camera tự động đồng bộ góc nhìn **Third-Person** của người chơi đang thi đấu và hỗ trợ chuyển đổi qua lại giữa các tay đua còn sống.
- **Phân định Thắng / Thua & Phần thưởng Xu:**
  - **Multiplayer:** Người đầu tiên chạm cổng **Checkwin**, hoặc **người chơi cuối cùng còn sống sót** khi tất cả đối thủ đã bị loại, sẽ thắng và nhận **+50 Xu**. Người thua nhận **0 Xu** và hiển thị trên bảng **Result**.
  - **Solo:** Vượt qua cổng **Checkwin** trước khi hết giờ để nhận **+10 Xu** và mở bảng chiến thắng tiếp tục chơi.

---

## 🎙️ 3. Giao Tiếp Thời Gian Thực: Voice Chat (Mic) & Text Chat

Ngay trên đường đua (góc dưới bên trái màn hình), người chơi có thể tương tác trực tiếp với nhau thông qua bộ đôi **Mic & Chat**:

- **Voice Chat (Photon Voice):**
  - Nút bật/tắt micro nhanh (**`MIC ON` / `MIC OFF`**) ngay trên giao diện thi đấu, hỗ trợ trò chuyện thoại độ trễ thấp với các tay đua khác trong cùng phòng.
- **Text Chat trong trận (`CHAT`):**
  - Bấm nút **`CHAT`** để nhập tin nhắn văn bản và nhấn `Enter` để gửi tới toàn bộ người chơi trong phòng.
  - Hệ thống tự động khóa điều khiển lái xe khi bạn đang gõ chữ trong ô chat để tránh xe vô tình chạy lệch khỏi đường đua.

---

## 🛒 4. Hệ Thống Cửa Hàng 3D & Tủ Đồ Sưu Tập (3D Shop & Inventory)

Từ màn hình Menu chính, người chơi truy cập **Cửa hàng (Shop)** để dùng số Xu tích lũy được mở khóa trang bị cá nhân hóa:

- **3 Danh mục vật phẩm (Tabs):**
  - **Scooter:** Các mẫu xe tay ga / scooter 3D đa dạng kiểu dáng và màu sắc.
  - **Helmet:** Các mẫu mũ bảo hiểm cá tính.
  - **Character:** Các nhân vật tay đua 3D.
- **Sân khấu trưng bày 3D xoay 360° (3D Showcase Stage):**
  - Tự động chuẩn hóa kích thước, căn giữa tâm hình học (*Bounds*) và vô hiệu hóa vật lý thừa để trưng bày mô hình 3D xoay mượt mà ngay giữa màn hình.
  - Chuyển qua lại giữa các vật phẩm (`Next` / `Previous`), xem tên, giá bán và mua trực tiếp bằng Xu (`BUY`) với thông báo trạng thái tức thì (`PURCHASED` / `NOT ENOUGH COINS` / `OWNED`).
- **Kệ trưng bày đồ đã sở hữu (Mini Owned Shelf):**
  - Ở góc trên bên trái màn hình Shop hiển thị **Kệ đồ 3D thu nhỏ (`OWNED ITEMS`)** chia 3 hàng (`SCOOTER`, `HELMET`, `CHAR`).
  - Toàn bộ vật phẩm đã mở khóa được nhân bản thu nhỏ, tự mở rộng khung kệ và xoay 3D để bạn theo dõi bộ sưu tập.

---

## 🎮 5. Nút Điều Khiển (Controls)

### Khi ở Sảnh chờ (Lobby):

| Thao tác / Phím bấm | Chức năng |
| :--- | :--- |
| Nút `SELECT VEHICLE` | Sang màn hình chọn xe (và map nếu là Chủ phòng). Chỉ hiện khi đã ở trong phòng |
| `Chuột phải` vào ô nhập mã | Dán nhanh mã phòng 5 ký tự từ Clipboard |
| `Enter` (trong ô nhập mã) | Tham gia phòng ngay lập tức (`Join Room`) |
| `ESC` | Rời phòng hiện tại hoặc quay về màn hình Menu chính |

### Khi ở màn hình Chọn xe (Select):

| Thao tác / Phím bấm | Chức năng |
| :--- | :--- |
| `←` / `A` hoặc nút `<` | Xe trước đó |
| `→` / `D` hoặc nút `>` | Xe kế tiếp |
| Nút `<` `>` dưới khung ảnh | Đổi map *(chỉ Chủ phòng)* |
| Nút xác nhận | Lưu lựa chọn và **quay lại Lobby (giữ nguyên phòng)** |
| `ESC` | Đang trong phòng: quay lại Lobby. Ngoài phòng: về Menu |

### Khi đang đua:
Sau khi cuộc đua bắt đầu, chỉ cần nhấn phím tăng tốc lần đầu, xe sẽ **duy trì ga chạy về phía trước**. Bạn có thể dùng **`WASD`** hoặc các phím **`Mũi tên`**:

| Phím bấm / Nút UI | Chức năng |
| :--- | :--- |
| `W` hoặc `↑` | **Khởi động & Tăng tốc (Boost):** Nhấn giữ để xe bứt tốc chạy nhanh hơn |
| `S` hoặc `↓` | **Phanh / Lùi xe:** Hãm tốc độ khi vào cua gắt hoặc lùi xe khi bị vướng chướng ngại vật |
| `A` hoặc `←` | **Rẽ trái** |
| `D` hoặc `→` | **Rẽ phải** |
| `Space` (Dấu cách) | **Nhảy:** Vượt chướng ngại vật hoặc căn góc nhảy qua đoạn cua |
| Nút `MIC OFF / ON` | Bật hoặc tắt micro (**Voice Chat**) |
| Nút `CHAT` + `Enter` | Mở khung trò chuyện văn bản (**Text Chat**) và gửi tin nhắn |
| `ESC` | **Tạm dừng / Thoát trận:** Mở bảng tùy chọn (`Resume` để tiếp tục hoặc `Quit` để rời phòng về Menu) |

### Khi ở chế độ Khán giả (Spectator Mode):

| Phím bấm / Nút | Chức năng |
| :--- | :--- |
| `E`, `→`, `Tab` hoặc nút `>` | Chuyển sang xem camera của tay đua tiếp theo |
| `Q`, `←` hoặc nút `<` | Quay lại xem camera của tay đua trước đó |
| `ESC` | Mở / đóng bảng tùy chọn **Ở lại xem** hoặc **Thoát trận** |

---

## 🗺️ 6. Hệ Thống Bản Đồ Thu Nhỏ (Minimap GPS)

- **Góc nhìn trực diện từ trên xuống:** Camera Minimap (Orthographic) bám sát xe của người chơi, giúp quan sát rõ toàn cảnh cung đường và các khúc cua phía trước.
- **Mũi tên định hướng GPS phân biệt người chơi:**
  - **Mũi tên Xanh lá (`Green`):** Vị trí và hướng di chuyển của **xe bạn**.
  - **Mũi tên Đỏ (`Red`):** Vị trí và hướng di chuyển của **xe đối thủ**.

---

## ⏱️ 7. Luật Chơi, Checkpoint & Hồi Sinh Khi Rơi Đường (Rules)

- **Thời gian ban đầu:** Mỗi tay đua có sẵn **30 giây** đếm ngược khi bắt đầu lượt chạy.
- **Đồng xu Checkpoint xoay tròn:** Mỗi khi ăn được 1 đồng xu checkpoint, bạn được **cộng thêm +5 giây** vào quỹ thời gian kèm thông báo và âm thanh.
- **Rơi khỏi đường đua — Quay về vạch xuất phát:** Nếu xe trượt khỏi đường đua và rơi xuống vùng nước biển bao quanh (`Sea_Plane` hoặc thấp hơn mực nước `Y <= -0.3`), xe sẽ **hồi sinh về vạch xuất phát** với vận tốc đặt lại về `0` (đồng hồ đếm ngược vẫn tiếp tục chạy).
- **Cổng Checkwin:** Giữ vững tay lái, thu thập checkpoint và cán đích tại **Cổng Checkwin** (biển hiệu **Billboard Finish**) sớm nhất để giành chiến thắng.
- **Điều kiện bị loại / thua cuộc:** Thời gian đếm ngược về `0` trước khi chạm đích, hoặc có đối thủ khác về đích trước bạn.

---

## 🔊 8. Hệ Thống Âm Thanh & Giao Diện Trực Quan (Audio & UI)

- **Nhạc nền xuyên suốt:** Nhạc nền tự động phát từ **Menu** sang **Lobby** và chuyển sang nhạc đường đua sôi động khi vào trận (tự tạm dừng khi mở bảng `ESC` ở chế độ Solo và dừng khi kết thúc ván).
- **Hiệu ứng âm thanh (SFX):** Âm thanh phản hồi riêng khi ăn Checkpoint, Chiến thắng (Win) và Bị loại / Thua cuộc (Game Over).
- **Hiển thị Xu & Bảng tin sự kiện:**
  - Luôn hiển thị **tổng số Xu hiện có** ở góc trên bên trái màn hình, đồng bộ vào các bảng kết quả và Cửa hàng.
  - Bảng tin thông báo tự gom nhóm và mờ dần khi có người bị loại, thoát phòng hoặc nhận thưởng thời gian.
- **Hiệu ứng nút bấm:** Nút phóng nhẹ khi rê chuột, nhún khi bấm và có viền sáng đổi màu theo màu chủ đạo của chiếc xe đang chọn.

---

## 💡 9. Mẹo Đua Xe (Pro Tips)

- **Vào phòng cực nhanh:** Chủ phòng bấm **Create** là mã 5 ký tự đã nằm sẵn trong Clipboard để `Ctrl + V` gửi bạn bè; người vào phòng chỉ cần **chuột phải** vào ô nhập mã là dán ngay.
- **Chọn xe sau khi vào phòng:** Bạn có thể đổi xe bất cứ lúc nào trước khi Chủ phòng bấm START GAME mà không bị mất phòng.
- **Bật Mic phối hợp hoặc "troll" bạn bè:** Bấm `MIC` ở góc dưới bên trái để nói chuyện trực tiếp với đồng đội / đối thủ khi đang bứt tốc.
- **Theo dõi mũi tên Đỏ trên Minimap:** Quan sát khoảng cách và hướng rẽ của đối thủ để chủ động bứt tốc ở các đoạn đường thẳng.
- **Giảm ga & phanh đúng lúc khi vào cua (`S` / `↓`):** Rơi khỏi đường đua sẽ bị đưa về vạch xuất phát và mất rất nhiều thời gian, hãy nhả ga hoặc rà phanh nhẹ trước các khúc cua gắt.
- **Cày Xu mở khóa Shop:** Chơi Multiplayer để nhận **+50 Xu/trận thắng**, mở khóa nhanh toàn bộ bộ sưu tập Scooter, Helmet và Character.

---

## 🛠️ 10. Công Nghệ Sử Dụng

- **Game Engine:** Unity 6 (Universal Render Pipeline - URP)
- **Networking & Voice:** Photon PUN 2 & Photon Voice 2 (Asia Region, Room Code, Map/Gear Custom Properties Sync & Host Migration)
- **Camera System:** Unity Cinemachine, Networked Third-Person Spectator Camera & Orthographic Minimap Camera
- **UI, 3D Showcase & Audio:** TextMeshPro (TMP), Dynamic 3D Bounds Normalization & Multi-channel AudioSource

---

## 📦 11. Tải Bản Build Game (BuildGame.zip)

👉 [Tải xuống BuildGame.zip tại đây](https://drive.google.com/drive/folders/19mJVw_RUIEru4CHCZbzZtJtPiwsEHSFR?usp=sharing)

---

<div align="center">

### ĐỒ ÁN MÔN HỌC - LẬP TRÌNH MẠNG CĂN BẢN
**NHÓM 4**

</div>
