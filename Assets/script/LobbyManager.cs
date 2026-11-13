using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Giao diện sảnh chờ")]
    public TMP_Text statusText;
    public TMP_InputField roomInput;
    public GameObject createButton;
    public GameObject joinButton;
    public GameObject startButton;

    [Header("Nút sao chép mã (tự tạo nếu để trống)")]
    public GameObject copyButton;
    public Vector2 viTriNutCopy = new Vector2(-20f, 20f);      // lệch so với góc dưới phải
    public Vector2 kichThuocNutCopy = new Vector2(190f, 46f);

    private TMP_Text nhanCopyText;
    private bool dangXuLy = false;
    private bool dangThoat = false;
    private Coroutine coroutineThongBao;
    private const int DO_DAI_MA_PHONG = 5;
    private const string KY_TU_MA_PHONG = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Bỏ O, 0, I, 1 tránh nhìn nhầm

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "asia";

        // Có tên để hiện trong danh sách phòng và thông báo trong trận
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 10000);

        if (startButton != null) startButton.SetActive(false);

        TaoNutCopyNeuChuaCo();
        if (copyButton != null) copyButton.SetActive(false);

        CauHinhOnhapMa();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        statusText.text = "Đang kết nối tới Server...";
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Đã kết nối! Bấm TẠO PHÒNG lấy mã, hoặc nhập mã để VÀO PHÒNG.";
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BamNutExit();
        }
    }

    // =========================================================================
    // Ô NHẬP MÃ: tự viết hoa, giới hạn 5 ký tự, Enter để vào phòng
    // =========================================================================
    private void CauHinhOnhapMa()
    {
        if (roomInput == null) return;

        roomInput.characterLimit = DO_DAI_MA_PHONG;

        // Cho phép nhấn chuột phải vào ô mã phòng để dán mã từ Clipboard.
        RoomInputPasteHandler pasteHandler =
            roomInput.GetComponent<RoomInputPasteHandler>();
        if (pasteHandler == null)
            pasteHandler = roomInput.gameObject.AddComponent<RoomInputPasteHandler>();
        pasteHandler.inputField = roomInput;

        roomInput.onValueChanged.RemoveAllListeners();
        roomInput.onValueChanged.AddListener(giaTri =>
        {
            string hoa = giaTri.ToUpper();
            if (hoa != giaTri) roomInput.text = hoa;
        });

        roomInput.onSubmit.RemoveAllListeners();
        roomInput.onSubmit.AddListener(_ => JoinRoom());
    }

    // =========================================================================
    // NÚT SAO CHÉP MÃ (nhỏ, góc dưới phải)
    // =========================================================================
    private void TaoNutCopyNeuChuaCo()
    {
        if (copyButton != null)
        {
            nhanCopyText = copyButton.GetComponentInChildren<TMP_Text>(true);
            Button btn = copyButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(CopyMaPhong);
                btn.onClick.AddListener(CopyMaPhong);
            }
            return;
        }

        Canvas canvas = statusText != null ? statusText.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;

        GameObject obj = new GameObject("CopyRoomButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(canvas.transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = viTriNutCopy;
        rect.sizeDelta = kichThuocNutCopy;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.1f, 0.6f, 0.8f, 0.95f);

        Button btnMoi = obj.GetComponent<Button>();
        btnMoi.targetGraphic = img;
        btnMoi.onClick.AddListener(CopyMaPhong);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform));
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "SAO CHÉP MÃ";
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        nhanCopyText = tmp;
        copyButton = obj;
    }

    // Gắn vào nút nếu bạn tự làm nút riêng
    public void CopyMaPhong()
    {
        if (!PhotonNetwork.InRoom) return;

        GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;

        if (nhanCopyText != null)
        {
            nhanCopyText.text = "ĐÃ SAO CHÉP!";
            CancelInvoke(nameof(DatLaiNhanCopy));
            Invoke(nameof(DatLaiNhanCopy), 1.5f);
        }
    }

    private void DatLaiNhanCopy()
    {
        if (nhanCopyText != null) nhanCopyText.text = "SAO CHÉP MÃ";
    }

    // =========================================================================
    // THOÁT / KẾT NỐI
    // =========================================================================
    public void BamNutExit()
    {
        if (dangXuLy || dangThoat) return;

        if (PhotonNetwork.InRoom)
        {
            dangXuLy = true;
            statusText.text = "Đang rời phòng...";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            dangThoat = true;
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            SceneManager.LoadScene(0);
        }
    }

    public override void OnLeftRoom()
    {
        dangXuLy = false;
        StopAllCoroutines();
        coroutineThongBao = null;

        HienGiaoDienChuaVaoPhong();
        statusText.text = "Đã rời phòng. Bấm TẠO PHÒNG hoặc nhập mã để VÀO PHÒNG.";
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Đã kết nối! Bấm TẠO PHÒNG lấy mã, hoặc nhập mã để VÀO PHÒNG.";
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (dangThoat) return;

        dangXuLy = false;
        StopAllCoroutines();
        coroutineThongBao = null;

        HienGiaoDienChuaVaoPhong();
        statusText.text = $"Mất kết nối ({cause}). Đang kết nối lại...";
        PhotonNetwork.ConnectUsingSettings();
    }

    private void HienGiaoDienChuaVaoPhong()
    {
        if (roomInput != null)
        {
            roomInput.text = "";
            roomInput.gameObject.SetActive(true);
        }
        if (createButton != null) createButton.SetActive(true);
        if (joinButton != null) joinButton.SetActive(true);
        if (startButton != null) startButton.SetActive(false);
        if (copyButton != null) copyButton.SetActive(false);
    }

    // =========================================================================
    // TẠO / VÀO PHÒNG
    // =========================================================================
    private string TaoMaPhongNgauNhien(int doDai = DO_DAI_MA_PHONG)
    {
        char[] ma = new char[doDai];
        for (int i = 0; i < doDai; i++)
        {
            ma[i] = KY_TU_MA_PHONG[Random.Range(0, KY_TU_MA_PHONG.Length)];
        }
        return new string(ma);
    }

    public void CreateRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Chưa kết nối xong Server, vui lòng đợi 1 giây...";
            return;
        }

        dangXuLy = true;
        string maPhongMoi = TaoMaPhongNgauNhien();
        statusText.text = $"Đang tạo phòng mã {maPhongMoi}...";

        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(maPhongMoi, roomOptions);
    }

    public void JoinRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Chưa kết nối xong Server, vui lòng đợi 1 giây...";
            return;
        }

        string maNhapVao = (roomInput != null) ? roomInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(maNhapVao))
        {
            statusText.text = "Vui lòng nhập MÃ PHÒNG trước khi bấm Vào!";
            return;
        }

        dangXuLy = true;
        statusText.text = $"Đang tìm phòng mã: {maNhapVao}...";
        PhotonNetwork.JoinRoom(maNhapVao);
    }

    public void StartGame()
    {
        if (dangXuLy || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        dangXuLy = true;
        if (startButton != null) startButton.SetActive(false);
        if (copyButton != null) copyButton.SetActive(false);
        statusText.text = "Đang tải đường đua...";
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel("game");
    }

    // =========================================================================
    // SỰ KIỆN TRONG PHÒNG
    // =========================================================================
    public override void OnJoinedRoom()
    {
        dangXuLy = false;

        if (roomInput != null) roomInput.gameObject.SetActive(false);
        if (createButton != null) createButton.SetActive(false);
        if (joinButton != null) joinButton.SetActive(false);
        if (copyButton != null) copyButton.SetActive(true);
        DatLaiNhanCopy();

        // Chủ phòng tự copy sẵn mã vào Clipboard
        if (PhotonNetwork.IsMasterClient)
        {
            GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;
        }

        HienTrangThaiPhong();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        HienThongBaoTamThoi($"<color=#00FF00>{LayTen(newPlayer)} đã vào phòng!</color>");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        HienThongBaoTamThoi($"<color=#FFFF00>{LayTen(otherPlayer)} đã thoát khỏi phòng!</color>");
    }

    // Chủ phòng thoát -> Photon chuyển chủ phòng cho người khác, cập nhật nút Start
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        HienThongBaoTamThoi(newMasterClient.IsLocal
            ? "<color=#00FFFF>Bạn là Chủ Phòng mới!</color>"
            : $"<color=#00FFFF>{LayTen(newMasterClient)} là Chủ Phòng mới!</color>");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;
        // Hiếm khi mã ngẫu nhiên bị trùng -> tự tạo lại mã khác
        if (returnCode == ErrorCode.GameIdAlreadyExists)
        {
            CreateRoom();
            return;
        }
        statusText.text = $"Lỗi tạo phòng ({message}). Hãy bấm Tạo lại!";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;

        if (returnCode == ErrorCode.GameDoesNotExist)
            statusText.text = "Không tìm thấy phòng này. Kiểm tra lại MÃ PHÒNG!";
        else if (returnCode == ErrorCode.GameFull)
            statusText.text = "Phòng đã đầy (tối đa 4 người)!";
        else if (returnCode == ErrorCode.GameClosed)
            statusText.text = "Trận đấu trong phòng này đã bắt đầu!";
        else
            statusText.text = "Không vào được phòng. Hãy thử lại!";
    }

    // =========================================================================
    // HIỂN THỊ TRẠNG THÁI PHÒNG
    // =========================================================================
    private string LayTen(Player p)
    {
        if (p == null) return "Người chơi";
        return string.IsNullOrEmpty(p.NickName) ? $"Người chơi #{p.ActorNumber}" : p.NickName;
    }

    private string TaoDanhSachNguoiChoi()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(LayTen(p));
            if (p.IsMasterClient) sb.Append(" (Chủ phòng)");
            if (p.IsLocal) sb.Append(" (Bạn)");
        }
        return sb.ToString();
    }

    private void HienTrangThaiPhong(string thongBao = null)
    {
        if (!PhotonNetwork.InRoom) return;

        Room phong = PhotonNetwork.CurrentRoom;
        bool laChuPhong = PhotonNetwork.IsMasterClient;

        if (startButton != null) startButton.SetActive(laChuPhong && !dangXuLy);

        string dauDong = string.IsNullOrEmpty(thongBao) ? "" : thongBao + "\n";
        string dongCuoi = laChuPhong
            ? "Gửi mã cho bạn bè rồi bấm START GAME!"
            : "Đang chờ Chủ Phòng bấm START GAME...";

        statusText.text =
            $"{dauDong}MÃ PHÒNG: <color=#00FFFF><b>{phong.Name}</b></color> ({phong.PlayerCount}/{phong.MaxPlayers})\n" +
            $"Người chơi: {TaoDanhSachNguoiChoi()}\n{dongCuoi}";
    }

    private void HienThongBaoTamThoi(string thongBao)
    {
        if (!PhotonNetwork.InRoom) return;

        if (coroutineThongBao != null) StopCoroutine(coroutineThongBao);
        coroutineThongBao = StartCoroutine(ChayThongBaoTamThoi(thongBao));
    }

    private IEnumerator ChayThongBaoTamThoi(string thongBao)
    {
        HienTrangThaiPhong(thongBao);

        yield return new WaitForSeconds(3.5f);

        coroutineThongBao = null;
        HienTrangThaiPhong();
    }
}

// Hỗ trợ dán mã phòng bằng chuột phải trong TMP_InputField.
public class RoomInputPasteHandler : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField inputField;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (inputField == null || !inputField.interactable)
            return;

        string clipboard = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(clipboard))
            return;

        // Mã phòng chỉ gồm 5 ký tự; bỏ khoảng trắng và chuyển thành chữ hoa.
        string maPhong = clipboard.Trim().ToUpper();
        if (maPhong.Length > 5)
            maPhong = maPhong.Substring(0, 5);

        inputField.text = maPhong;
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }
}

