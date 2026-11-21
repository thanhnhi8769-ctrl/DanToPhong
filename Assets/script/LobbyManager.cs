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

    [Header("Chọn xe / map (nút chỉ hiện khi đã vào phòng)")]
    public GameObject selectButton;
    [Tooltip("Tên scene Select. PHẢI chứa chữ 'select' (vd: SelectScene)")]
    public string tenSceneSelect = "SelectScene";

    [Header("Nút sao chép mã (tự tạo nếu để trống)")]
    public GameObject copyButton;
    public Vector2 viTriNutCopy = new Vector2(-20f, 20f);
    public Vector2 kichThuocNutCopy = new Vector2(190f, 46f);

    [Header("Hiệu ứng nút")]
    public bool hieuUngNut = true;
    public Color mauVienNut = new Color(0.15f, 0.95f, 0.5f);

    [Header("Kết nối")]
    public int soLanThuKetNoiLaiToiDa = 3;
    public float thoiGianChoKetNoi = 10f;
    public float thoiGianChoLenh = 8f;

    [Header("Đếm ngược trước khi vào scene game")]
    [Tooltip("Số giây đếm ngược sau khi Chủ Phòng bấm START GAME (mọi người trong phòng đều thấy). Tăng lên nếu máy tải map chậm.")]
    public float thoiGianDemNguocVaoGame = 3f;
    public string mauChuDemNguoc = "<color=#FFD54F><b>Game starts in {0}s...</b></color>\nGet ready!";

    private TMP_Text nhanCopyText;
    private bool dangXuLy = false;
    private bool dangThoat = false;
    private bool dangDem = false;
    private bool daGoiNapScene = false;
    private int giayDemCuoi = -1;
    private int soLanThuLai = 0;
    private Coroutine coroutineThongBao;
    private Coroutine coChoLenh;
    private Button startBtnCache;
    private readonly StringBuilder sb = new StringBuilder(128);

    private static bool dangQuaSceneChonXe = false;

    private const int DO_DAI_MA_PHONG = 5;
    private const string KY_TU_MA_PHONG = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string TIN_NHAN_MAT_MANG = "Không kết nối được Server. Kiểm tra Internet.\nNhấn ESC để quay lại menu.";
    private const string KEY_DANG_CHON_XE = "DangChonXe";   // SelectManager gắn khi người chơi đang ở màn chọn xe
    private const string KEY_DEM_BAT_DAU = "DemBatDau";     // Room property: thời điểm (PhotonNetwork.Time) bắt đầu đếm ngược
    private const string KEY_DEM_GIAY = "DemGiay";          // Room property: số giây đếm ngược
    private const string KEY_DA_NAP = "DaNapScene";         // Room property: Chủ phòng đã gọi nạp scene game

    private void Awake()
    {
        if (GetComponent<PhotonView>() == null)
        {
            PhotonView pv = gameObject.AddComponent<PhotonView>();
            pv.ViewID = 998;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "asia";

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 10000);

        if (startButton != null)
        {
            startBtnCache = startButton.GetComponent<Button>();
            startButton.SetActive(false);
        }

        if (selectButton != null)
        {
            Button sbtn = selectButton.GetComponent<Button>();
            if (sbtn == null) sbtn = selectButton.GetComponentInChildren<Button>(true);

            if (sbtn != null)
            {
                sbtn.onClick = new Button.ButtonClickedEvent();
                sbtn.onClick.AddListener(MoSceneChonXe);
            }
            selectButton.SetActive(false);
        }

        TaoNutCopyNeuChuaCo();
        if (copyButton != null) copyButton.SetActive(false);

        if (hieuUngNut) GanHieuUngChoMoiNut();

        CauHinhOnhapMa();

        if (PhotonNetwork.InRoom)
        {
            if (dangQuaSceneChonXe || PhotonNetwork.CurrentRoom.IsOpen)
            {
                dangQuaSceneChonXe = false;
                HienGiaoDienTrongPhong();
                return;
            }
            PhotonNetwork.LeaveRoom();
        }

        dangQuaSceneChonXe = false;
        BatDauKetNoi();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BamNutExit();
        }
    }

    private void GanHieuUngChoMoiNut()
    {
        ButtonFx.MauChung = mauVienNut;

        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.GetComponent<ButtonFx>() == null) b.gameObject.AddComponent<ButtonFx>();
        }
    }

    // =========================================================================
    // KẾT NỐI
    // =========================================================================
    private void BatDauKetNoi()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Đã kết nối! Bấm TẠO PHÒNG lấy mã, hoặc nhập mã để VÀO PHÒNG.";
            return;
        }

        statusText.text = "Đang kết nối tới Server...";
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        StartCoroutine(GiamSatKetNoi());
    }

    private IEnumerator GiamSatKetNoi()
    {
        yield return new WaitForSecondsRealtime(thoiGianChoKetNoi);

        if (!PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !dangThoat)
            statusText.text = TIN_NHAN_MAT_MANG;
    }

    private IEnumerator KetNoiLaiSau(float giay)
    {
        yield return new WaitForSecondsRealtime(giay);

        if (dangThoat || PhotonNetwork.IsConnected) yield break;
        PhotonNetwork.ConnectUsingSettings();
        StartCoroutine(GiamSatKetNoi());
    }

    // =========================================================================
    // Ô NHẬP MÃ
    // =========================================================================
    private void CauHinhOnhapMa()
    {
        if (roomInput == null) return;

        roomInput.characterLimit = DO_DAI_MA_PHONG;

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
    // NÚT SAO CHÉP MÃ
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
    // CHỌN XE / MAP
    // =========================================================================
    public void MoSceneChonXe()
    {
        if (!PhotonNetwork.InRoom)
        {
            statusText.text = "Hãy TẠO PHÒNG hoặc VÀO PHÒNG trước khi chọn xe!";
            return;
        }
        if (dangXuLy || dangThoat || dangDem) return;

        if (!Application.CanStreamedLevelBeLoaded(tenSceneSelect))
        {
            string loi = $"Không tìm thấy scene '{tenSceneSelect}'. Thêm nó vào File > Build Settings!";
            statusText.text = loi;
            return;
        }

        dangQuaSceneChonXe = true;
        StopAllCoroutines();
        PhotonNetwork.AutomaticallySyncScene = false;
        SceneManager.LoadScene(tenSceneSelect);
    }

    private void HienGiaoDienTrongPhong()
    {
        dangXuLy = false;
        HuyChoLenh();

        if (roomInput != null) roomInput.gameObject.SetActive(false);
        if (createButton != null) createButton.SetActive(false);
        if (joinButton != null) joinButton.SetActive(false);
        if (copyButton != null) copyButton.SetActive(true);
        if (selectButton != null) selectButton.SetActive(true);
        DatLaiNhanCopy();

        HienTrangThaiPhong();
    }

    // =========================================================================
    // THOÁT
    // =========================================================================
    public void BamNutExit()
    {
        if (dangThoat) return;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        {
            dangXuLy = true;
            statusText.text = "Đang rời phòng...";
            PhotonNetwork.LeaveRoom();
            return;
        }

        dangThoat = true;
        StopAllCoroutines();
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public override void OnLeftRoom()
    {
        dangXuLy = false;
        dangDem = false;
        daGoiNapScene = false;
        giayDemCuoi = -1;
        StopAllCoroutines();
        coroutineThongBao = null;
        coChoLenh = null;

        HienGiaoDienChuaVaoPhong();
        statusText.text = "Đã rời phòng. Bấm TẠO PHÒNG hoặc nhập mã để VÀO PHÒNG.";
    }

    public override void OnConnectedToMaster()
    {
        soLanThuLai = 0;
        statusText.text = "Đã kết nối! Bấm TẠO PHÒNG lấy mã, hoặc nhập mã để VÀO PHÒNG.";
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (dangThoat) return;

        dangXuLy = false;
        dangDem = false;
        daGoiNapScene = false;
        giayDemCuoi = -1;
        HuyChoLenh();
        coroutineThongBao = null;

        HienGiaoDienChuaVaoPhong();

        if (soLanThuLai < soLanThuKetNoiLaiToiDa)
        {
            soLanThuLai++;
            statusText.text = $"Mất kết nối ({cause}). Thử lại {soLanThuLai}/{soLanThuKetNoiLaiToiDa}...";
            StartCoroutine(KetNoiLaiSau(2f));
        }
        else
        {
            statusText.text = TIN_NHAN_MAT_MANG;
        }
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
        if (selectButton != null) selectButton.SetActive(false);
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

    private string LyDoChuaSanSang()
    {
        if (!PhotonNetwork.IsConnected) return TIN_NHAN_MAT_MANG;
        return "Chưa kết nối xong Server, vui lòng đợi 1 giây...";
    }

    private void BatDauChoLenh()
    {
        HuyChoLenh();
        coChoLenh = StartCoroutine(HetGioChoLenh());
    }

    private void HuyChoLenh()
    {
        if (coChoLenh != null)
        {
            StopCoroutine(coChoLenh);
            coChoLenh = null;
        }
    }

    private IEnumerator HetGioChoLenh()
    {
        yield return new WaitForSecondsRealtime(thoiGianChoLenh);

        coChoLenh = null;
        if (dangXuLy && !PhotonNetwork.InRoom)
        {
            dangXuLy = false;
            statusText.text = "Server không phản hồi. Hãy thử lại hoặc nhấn ESC để quay lại.";
        }
    }

    public void CreateRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Vui lòng đợi {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s để game tải lại...";
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = LyDoChuaSanSang();
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

        if (PhotonNetwork.CreateRoom(maPhongMoi, roomOptions))
        {
            BatDauChoLenh();
        }
        else
        {
            dangXuLy = false;
            statusText.text = "Không gửi được lệnh tạo phòng. Hãy thử lại!";
        }
    }

    public void JoinRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Vui lòng đợi {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s để game tải lại...";
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = LyDoChuaSanSang();
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

        if (PhotonNetwork.JoinRoom(maNhapVao))
        {
            BatDauChoLenh();
        }
        else
        {
            dangXuLy = false;
            statusText.text = "Không gửi được lệnh vào phòng. Hãy thử lại!";
        }
    }

    // Còn ai đang ở màn chọn xe không? (tenNguoi = danh sách tên, cách nhau bằng dấu phẩy)
    private bool CoNguoiDangChonXe(out string tenNguoi)
    {
        sb.Clear();
        Player[] ds = PhotonNetwork.PlayerList;
        for (int i = 0; i < ds.Length; i++)
        {
            Player p = ds[i];
            if (p.CustomProperties.TryGetValue(KEY_DANG_CHON_XE, out object v) && v is bool b && b)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(LayTen(p));
            }
        }
        tenNguoi = sb.ToString();
        return tenNguoi.Length > 0;
    }

    // =========================================================================
    // START GAME + ĐẾM NGƯỢC TRƯỚC KHI VÀO SCENE GAME
    // Chủ phòng bấm START -> đóng phòng, báo giờ đếm ngược cho mọi người -> hết giờ mới nạp scene.
    // =========================================================================
    public void StartGame()
    {
        if (dangXuLy || dangDem || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Vui lòng đợi {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s để game tải lại...";
            return;
        }

        // Mọi người phải chọn xe xong, nếu không họ sẽ vào game lệch pha và không thấy nhau
        if (CoNguoiDangChonXe(out string dangChon))
        {
            statusText.text = $"Chưa thể bắt đầu: {dangChon} vẫn đang chọn xe...";
            return;
        }

        dangXuLy = true;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;

        string map = PlayerInventory.GetSelectedMap();
        if (string.IsNullOrEmpty(map) || map == "game" || !Application.CanStreamedLevelBeLoaded(map))
            map = GameNetworkManager.LayTenSceneVaoGame();

        float giayDem = Mathf.Max(0.2f, thoiGianDemNguocVaoGame);
        double mocBatDau = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "Map", map },
            { "SoNguoi", PhotonNetwork.CurrentRoom.PlayerCount },
            { KEY_DEM_BAT_DAU, mocBatDau },
            { KEY_DEM_GIAY, giayDem }
        });

        BatGiaoDienDemNguoc();
        StartCoroutine(DemNguocRoiVaoGame(map, giayDem, mocBatDau));
    }

    private void BatGiaoDienDemNguoc()
    {
        dangDem = true;
        giayDemCuoi = -1;

        HuyChoLenh();
        if (coroutineThongBao != null) { StopCoroutine(coroutineThongBao); coroutineThongBao = null; }

        if (startButton != null) startButton.SetActive(false);
        if (copyButton != null) copyButton.SetActive(false);
        if (selectButton != null) selectButton.SetActive(false);
    }

    private void CapNhatChuDemNguoc(float conLai)
    {
        int giay = Mathf.Max(1, Mathf.CeilToInt(conLai));
        if (giay == giayDemCuoi) return;
        giayDemCuoi = giay;
        statusText.text = string.Format(mauChuDemNguoc, giay);
    }

    // Chủ phòng: đếm ngược rồi nạp scene cho cả phòng
    private IEnumerator DemNguocRoiVaoGame(string map, float giayDem, double batDau)
    {
        // Dùng giờ Photon để khớp với máy khách; hết giờ mới nạp scene (CustomProperties đã kịp đồng bộ)
        float conLai = giayDem - (float)(PhotonNetwork.Time - batDau);

        while (conLai > 0f)
        {
            CapNhatChuDemNguoc(conLai);
            yield return null;
            conLai = giayDem - (float)(PhotonNetwork.Time - batDau);
        }

        statusText.text = "Đang tải đường đua...";

        // Đánh dấu đã nạp scene: nếu Chủ phòng rớt mạng ngay sau đó, Chủ phòng mới không nạp lần 2
        daGoiNapScene = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_DA_NAP, true } });
        PhotonNetwork.LoadLevel(map);
    }

    // Đang đếm ngược mà còn dưới 2 người: hủy trận, mở lại phòng, mọi người về màn phòng chờ như bình thường
    private void HuyDemNguoc(string lyDo)
    {
        StopAllCoroutines();
        coroutineThongBao = null;
        coChoLenh = null;
        dangDem = false;
        dangXuLy = false;
        giayDemCuoi = -1;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { KEY_DEM_BAT_DAU, null },
                { KEY_DEM_GIAY, null }
            });
        }

        HienGiaoDienTrongPhong();
        HienThongBaoTamThoi(lyDo);
    }

    private bool KhongDuNguoiDeVaoGame()
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < 2;
    }

    // Chủ phòng cũ thoát giữa lúc đếm ngược: Chủ phòng MỚI (Photon tự chuyển) làm tiếp phần còn lại
    private bool TiepQuanDemNguoc()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return false;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.ContainsKey(KEY_DEM_BAT_DAU) || !props.ContainsKey(KEY_DEM_GIAY) || !props.ContainsKey("Map"))
            return false;
        if (props.ContainsKey(KEY_DA_NAP)) return false;

        string map = props["Map"] as string;
        if (string.IsNullOrEmpty(map) || !Application.CanStreamedLevelBeLoaded(map)) return false;

        if (KhongDuNguoiDeVaoGame())
        {
            HuyDemNguoc("<color=#FFFF00>Không đủ người chơi (cần ít nhất 2). Đã hủy đếm ngược!</color>");
            return true;
        }

        double batDau = System.Convert.ToDouble(props[KEY_DEM_BAT_DAU]);
        float giay = System.Convert.ToSingle(props[KEY_DEM_GIAY]);

        StopAllCoroutines();           // dừng phần hiển thị đếm ngược của máy khách
        coroutineThongBao = null;
        coChoLenh = null;

        dangXuLy = true;
        PhotonNetwork.AutomaticallySyncScene = true;
        BatGiaoDienDemNguoc();
        StartCoroutine(DemNguocRoiVaoGame(map, giay, batDau));
        return true;
    }

    // Máy khách: chỉ hiển thị đếm ngược, việc nạp scene do Chủ phòng làm (AutomaticallySyncScene)
    private IEnumerator HienDemNguocMayKhach(double batDau, float giayDem)
    {
        float conLai = giayDem - (float)(PhotonNetwork.Time - batDau);

        while (conLai > 0f)
        {
            CapNhatChuDemNguoc(conLai);
            yield return null;
            conLai = giayDem - (float)(PhotonNetwork.Time - batDau);
        }

        statusText.text = "Đang tải đường đua...";
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null || dangDem) return;
        if (PhotonNetwork.IsMasterClient) return;      // chủ phòng tự chạy đếm ngược của mình
        if (!propertiesThatChanged.ContainsKey(KEY_DEM_BAT_DAU)) return;

        object vBatDau = propertiesThatChanged[KEY_DEM_BAT_DAU];
        object vGiay = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_DEM_GIAY)
            ? PhotonNetwork.CurrentRoom.CustomProperties[KEY_DEM_GIAY]
            : null;
        if (vBatDau == null || vGiay == null) return;

        BatGiaoDienDemNguoc();
        StartCoroutine(HienDemNguocMayKhach(System.Convert.ToDouble(vBatDau), System.Convert.ToSingle(vGiay)));
    }

    // =========================================================================
    // SỰ KIỆN TRONG PHÒNG
    // =========================================================================
    public override void OnJoinedRoom()
    {
        HienGiaoDienTrongPhong();

        if (PhotonNetwork.IsMasterClient)
        {
            GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        HienThongBaoTamThoi($"<color=#00FF00>{LayTen(newPlayer)} đã vào phòng!</color>");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (dangDem && !daGoiNapScene && KhongDuNguoiDeVaoGame())
        {
            HuyDemNguoc($"<color=#FFFF00>{LayTen(otherPlayer)} đã thoát. Không đủ người chơi (cần ít nhất 2), đã hủy đếm ngược!</color>");
            return;
        }

        HienThongBaoTamThoi($"<color=#FFFF00>{LayTen(otherPlayer)} đã thoát khỏi phòng!</color>");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient != null && newMasterClient.IsLocal && TiepQuanDemNguoc()) return;

        HienThongBaoTamThoi(newMasterClient.IsLocal
            ? "<color=#00FFFF>Bạn là Chủ Phòng mới!</color>"
            : $"<color=#00FFFF>{LayTen(newMasterClient)} là Chủ Phòng mới!</color>");
    }

    // Có người vào / ra khỏi màn chọn xe -> cập nhật ngay nút Start và dòng trạng thái
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps != null && changedProps.ContainsKey(KEY_DANG_CHON_XE))
            HienTrangThaiPhong();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;
        HuyChoLenh();

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
        HuyChoLenh();

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
        sb.Clear();
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
        if (dangDem) return;           // đang đếm ngược: không ghi đè chữ đếm ngược

        Room phong = PhotonNetwork.CurrentRoom;
        bool laChuPhong = PhotonNetwork.IsMasterClient;
        bool conNguoiChonXe = CoNguoiDangChonXe(out string tenDangChon);

        if (startButton != null)
        {
            startButton.SetActive(laChuPhong && !dangXuLy);
            if (startBtnCache == null) startBtnCache = startButton.GetComponent<Button>();
            if (startBtnCache != null) startBtnCache.interactable = !conNguoiChonXe;
        }

        string dauDong = string.IsNullOrEmpty(thongBao) ? "" : thongBao + "\n";
        string dongCuoi = laChuPhong
            ? "Bấm CHỌN XE để chọn xe + map, gửi mã cho bạn bè rồi bấm START GAME!"
            : "Bấm CHỌN XE để chọn xe. Đang chờ Chủ Phòng bấm START GAME...";

        if (conNguoiChonXe)
            dongCuoi += $"\n<color=#FFD54F>Đang chờ {tenDangChon} chọn xe xong...</color>";

        // Dựng chuỗi trước khi LayTen/TaoDanhSachNguoiChoi dùng chung StringBuilder
        string danhSach = TaoDanhSachNguoiChoi();

        statusText.text =
            $"{dauDong}MÃ PHÒNG: <color=#00FFFF><b>{phong.Name}</b></color> ({phong.PlayerCount}/{phong.MaxPlayers})\n" +
            $"Người chơi: {danhSach}\n{dongCuoi}";
    }

    private void HienThongBaoTamThoi(string thongBao)
    {
        if (!PhotonNetwork.InRoom || dangDem) return;

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

        string maPhong = clipboard.Trim().ToUpper();
        if (maPhong.Length > 5)
            maPhong = maPhong.Substring(0, 5);

        inputField.text = maPhong;
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }
}