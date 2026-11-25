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
    [Header("Lobby UI")]
    public TMP_Text statusText;
    public TMP_InputField roomInput;
    public GameObject createButton;
    public GameObject joinButton;
    public GameObject startButton;

    [Header("Vehicle / Map Selection (button only shown when in room)")]
    public GameObject selectButton;
    [Tooltip("Select scene name. MUST contain the word 'select' (e.g., SelectScene)")]
    public string tenSceneSelect = "SelectScene";

    [Header("Copy Code Button (auto-created if left empty)")]
    public GameObject copyButton;
    public Vector2 viTriNutCopy = new Vector2(-20f, 20f);
    public Vector2 kichThuocNutCopy = new Vector2(190f, 46f);

    [Header("Button Effects")]
    public bool hieuUngNut = true;
    public Color mauVienNut = new Color(0.15f, 0.95f, 0.5f);

    [Header("Connection")]
    public int soLanThuKetNoiLaiToiDa = 3;
    public float thoiGianChoKetNoi = 10f;
    public float thoiGianChoLenh = 8f;

    [Header("Countdown Before Entering Game Scene")]
    [Tooltip("Countdown duration in seconds after the Room Host clicks START GAME (visible to everyone in the room). Increase if map loading is slow.")]
    public float thoiGianDemNguocVaoGame = 3f;
    public string mauChuDemNguoc = "<color=#FFD54F><b>Game starts in {0}s...</b></color>\nGet ready!";

    [Header("Track Loading")]
    [Tooltip("If the track scene is still not loaded after this many seconds, show an error instead of hanging on 'Loading track...'.")]
    public float thoiGianToiDaNapMap = 20f;

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

    // Launcher: tự tạo phòng / tự vào phòng theo mã (dùng 1 lần)
    private bool daTuDongTheoLauncher;

    private static bool dangQuaSceneChonXe = false;
    private static bool coSanPhongTuVanTruoc = false;

    public static void DanhDauCoSanPhongTuVanTruoc()
    {
        coSanPhongTuVanTruoc = true;
    }

    private const int DO_DAI_MA_PHONG = 5;
    private const string KY_TU_MA_PHONG = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string TIN_NHAN_MAT_MANG = "Unable to connect to Server. Check your Internet connection.\nPress ESC to return to the menu.";
    private const string KEY_DANG_CHON_XE = "DangChonXe";
    private const string KEY_DEM_BAT_DAU = "DemBatDau";
    private const string KEY_DEM_GIAY = "DemGiay";
    private const string KEY_DA_NAP = "DaNapScene";

    // UI button labels: MUST match the actual button text so players aren't confused
    private const string TEN_NUT_CHON_XE = "SELECT VEHICLE & MAP";
    private const string TEN_NUT_START = "START GAME";
    private const string TEN_NUT_CONFIRM = "CONFIRM";

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

        // FIX: khi quay về từ Select thì tắt sync, chỉ bật lại khi chủ phòng bấm START
        PhotonNetwork.AutomaticallySyncScene = !(coSanPhongTuVanTruoc && PhotonNetwork.InRoom);

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

        if (coSanPhongTuVanTruoc && PhotonNetwork.InRoom)
        {
            coSanPhongTuVanTruoc = false;
            ChuanBiLaiPhongChoVangMoi();
            HienGiaoDienTrongPhong();
            return;
        }

        coSanPhongTuVanTruoc = false;
        dangQuaSceneChonXe = false;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        BatDauKetNoi();

        // Đã kết nối sẵn thì OnJoinedLobby sẽ không chạy lại, nên gọi ở đây
        if (PhotonNetwork.IsConnectedAndReady) TuDongTheoLauncher();
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
            statusText.text = "Connected! Click CREATE ROOM to get a code, or enter a code and click JOIN ROOM.";
            return;
        }

        statusText.text = "Connecting to Server...";
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
    // LAUNCHER: TỰ TẠO PHÒNG / TỰ VÀO PHÒNG THEO MÃ
    // =========================================================================
    public override void OnJoinedLobby()
    {
        TuDongTheoLauncher();
    }

    // Launcher: mode "create" -> tự tạo phòng; mode "room" + mã -> tự vào phòng. Dùng 1 lần.
    private void TuDongTheoLauncher()
    {
        if (daTuDongTheoLauncher || !LauncherArgs.HasLauncher) return;

        string mode = LauncherArgs.Mode;
        string room = LauncherArgs.Room;
        if (mode != "create" && !(mode == "room" && !string.IsNullOrEmpty(room))) return;

        daTuDongTheoLauncher = true;
        LauncherArgs.Mode = "multiplayer";   // đã dùng, lần sau vào Lobby sẽ không tự tạo/vào nữa
        LauncherArgs.Room = "";

        if (mode == "create") CreateRoom();
        else VaoPhongTheoMa(room);
    }

    // InviteListener cũng gọi hàm này khi người chơi bấm JOIN ở lời mời
    public void VaoPhongTheoMa(string ma)
    {
        if (roomInput != null) roomInput.text = (ma ?? "").Trim().ToUpper();
        JoinRoom();
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
        tmp.text = "COPY CODE";
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
            nhanCopyText.text = "COPIED!";
            CancelInvoke(nameof(DatLaiNhanCopy));
            Invoke(nameof(DatLaiNhanCopy), 1.5f);
        }
    }

    private void DatLaiNhanCopy()
    {
        if (nhanCopyText != null) nhanCopyText.text = "COPY CODE";
    }

    // =========================================================================
    // VEHICLE / MAP SELECTION
    // =========================================================================
    public void MoSceneChonXe()
    {
        if (!PhotonNetwork.InRoom)
        {
            statusText.text = $"Please CREATE ROOM or JOIN ROOM before clicking {TEN_NUT_CHON_XE}!";
            return;
        }
        if (dangXuLy || dangThoat || dangDem) return;

        if (!Application.CanStreamedLevelBeLoaded(tenSceneSelect))
        {
            string loi = $"Scene '{tenSceneSelect}' not found. Add it to File > Build Settings!";
            statusText.text = loi;
            return;
        }

        dangQuaSceneChonXe = true;
        StopAllCoroutines();
        PhotonNetwork.AutomaticallySyncScene = false;
        SceneManager.LoadScene(tenSceneSelect);
    }

    private void ChuanBiLaiPhongChoVangMoi()
    {
        dangXuLy = false;
        dangDem = false;
        daGoiNapScene = false;
        giayDemCuoi = -1;
        dangQuaSceneChonXe = false;

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { KEY_DANG_CHON_XE, false }
                });
            }

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { KEY_DEM_BAT_DAU, null },
                    { KEY_DEM_GIAY, null },
                    { KEY_DA_NAP, null }
                });
            }
        }
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

        coSanPhongTuVanTruoc = false;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        {
            dangXuLy = true;
            statusText.text = "Leaving room...";
            PhotonNetwork.LeaveRoom();
            return;
        }

        dangThoat = true;
        StopAllCoroutines();
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        Time.timeScale = 1f;
        DieuHuong.VeMenu();   // về scene menu (KHÔNG thoát game); chỉ nút QUIT ở menu mới thoát
    }

    public override void OnLeftRoom()
    {
        coSanPhongTuVanTruoc = false;
        dangXuLy = false;
        dangDem = false;
        daGoiNapScene = false;
        giayDemCuoi = -1;
        StopAllCoroutines();
        coroutineThongBao = null;
        coChoLenh = null;

        HienGiaoDienChuaVaoPhong();
        statusText.text = "Left the room. Click CREATE ROOM or enter a code to JOIN ROOM.";
    }

    public override void OnConnectedToMaster()
    {
        soLanThuLai = 0;
        statusText.text = "Connected! Click CREATE ROOM to get a code, or enter a code to JOIN ROOM.";
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (dangThoat) return;

        coSanPhongTuVanTruoc = false;
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
            statusText.text = $"Disconnected ({cause}). Retrying {soLanThuLai}/{soLanThuKetNoiLaiToiDa}...";
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
        return "Not finished connecting to the Server, please wait 1 second...";
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
            statusText.text = "Server not responding. Please try again or press ESC to go back.";
        }
    }

    public void CreateRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Please wait {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s for the game to reload...";
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = LyDoChuaSanSang();
            return;
        }

        dangXuLy = true;
        string maPhongMoi = TaoMaPhongNgauNhien();
        statusText.text = $"Creating room with code {maPhongMoi}...";

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
            statusText.text = "Failed to send create room request. Please try again!";
        }
    }

    public void JoinRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Please wait {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s for the game to reload...";
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
            statusText.text = "Please enter the ROOM CODE before clicking JOIN ROOM!";
            return;
        }

        dangXuLy = true;
        statusText.text = $"Searching for room code: {maNhapVao}...";

        if (PhotonNetwork.JoinRoom(maNhapVao))
        {
            BatDauChoLenh();
        }
        else
        {
            dangXuLy = false;
            statusText.text = "Failed to send room request. Please try again!";
        }
    }

    private int DemNguoiDangChonXe(out string tenNguoi)
    {
        sb.Clear();
        int soDangChon = 0;
        Player[] ds = PhotonNetwork.PlayerList;
        for (int i = 0; i < ds.Length; i++)
        {
            Player p = ds[i];
            if (p.CustomProperties.TryGetValue(KEY_DANG_CHON_XE, out object v) && v is bool b && b)
            {
                soDangChon++;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(LayTen(p));
            }
        }
        tenNguoi = sb.ToString();
        return soDangChon;
    }

    private bool CoNguoiDangChonXe(out string tenNguoi)
    {
        return DemNguoiDangChonXe(out tenNguoi) > 0;
    }

    // =========================================================================
    // START GAME + ĐẾM NGƯỢC
    // =========================================================================
    public void StartGame()
    {
        if (dangXuLy || dangDem || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        if (VaoGameCooldown.ConLai > 0f)
        {
            statusText.text = $"Please wait {Mathf.CeilToInt(VaoGameCooldown.ConLai)}s for the game to reload...";
            return;
        }

        if (CoNguoiDangChonXe(out string dangChon))
        {
            statusText.text = $"Cannot start yet: {dangChon} is still in {TEN_NUT_CHON_XE} (has not clicked {TEN_NUT_CONFIRM})...";
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

        FindFirstObjectByType<LobbyInviteUI>()?.HideForStart();

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

    private IEnumerator DemNguocRoiVaoGame(string map, float giayDem, double batDau)
    {
        float conLai = giayDem - (float)(PhotonNetwork.Time - batDau);

        while (conLai > 0f)
        {
            CapNhatChuDemNguoc(conLai);
            yield return null;
            conLai = giayDem - (float)(PhotonNetwork.Time - batDau);
        }

        statusText.text = "Loading track...";

        // Scene không có trong Build Settings -> thử scene mặc định
        if (!Application.CanStreamedLevelBeLoaded(map))
        {
            string fallback = GameNetworkManager.LayTenSceneVaoGame();
            Debug.LogError($"[Lobby] Scene '{map}' is NOT in Build Settings. Fallback -> '{fallback}'");
            map = fallback;
        }

        // Vẫn không có -> báo lỗi và mở lại phòng, đừng để kẹt
        if (!Application.CanStreamedLevelBeLoaded(map))
        {
            Debug.LogError($"[Lobby] No map or vehicle selected. Press SELECT MAP & VEHICLE to choose.");
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
            statusText.text = $"No map or vehicle selected. Press SELECT MAP & VEHICLE to choose";
            yield break;
        }

        Debug.Log($"[Lobby] LoadLevel('{map}') master={PhotonNetwork.IsMasterClient} sync={PhotonNetwork.AutomaticallySyncScene} players={PhotonNetwork.CurrentRoom.PlayerCount}");

        daGoiNapScene = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_DA_NAP, true } });
        PhotonNetwork.LoadLevel(map);

        // Nếu nạp scene thành công thì object này bị hủy và đoạn dưới không chạy.
        // Còn chạy tới đây nghĩa là scene chưa nạp được -> báo lỗi thay vì treo mãi.
        yield return new WaitForSecondsRealtime(Mathf.Max(5f, thoiGianToiDaNapMap));
        Debug.LogError($"[Lobby] Track '{map}' did not load within {thoiGianToiDaNapMap}s. Check the Console / Player.log for Photon errors.");
        if (statusText != null)
            statusText.text = "<color=#FF6B6B>Track failed to load.</color>\nPress ESC to leave the room and try again.";
    }

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
            HuyDemNguoc("<color=#FFFF00>Not enough players (at least 2 required). Countdown canceled!</color>");
            return true;
        }

        double batDau = System.Convert.ToDouble(props[KEY_DEM_BAT_DAU]);
        float giay = System.Convert.ToSingle(props[KEY_DEM_GIAY]);

        StopAllCoroutines();
        coroutineThongBao = null;
        coChoLenh = null;

        dangXuLy = true;
        PhotonNetwork.AutomaticallySyncScene = true;
        BatGiaoDienDemNguoc();
        StartCoroutine(DemNguocRoiVaoGame(map, giay, batDau));
        return true;
    }

    private IEnumerator HienDemNguocMayKhach(double batDau, float giayDem)
    {
        float conLai = giayDem - (float)(PhotonNetwork.Time - batDau);

        while (conLai > 0f)
        {
            CapNhatChuDemNguoc(conLai);
            yield return null;
            conLai = giayDem - (float)(PhotonNetwork.Time - batDau);
        }

        statusText.text = "Loading track...";
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null || dangDem) return;
        if (PhotonNetwork.IsMasterClient) return;
        if (!propertiesThatChanged.ContainsKey(KEY_DEM_BAT_DAU)) return;

        object vBatDau = propertiesThatChanged[KEY_DEM_BAT_DAU];
        object vGiay = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_DEM_GIAY)
            ? PhotonNetwork.CurrentRoom.CustomProperties[KEY_DEM_GIAY]
            : null;
        if (vBatDau == null || vGiay == null) return;

        PhotonNetwork.AutomaticallySyncScene = true; // FIX: chủ phòng đã START, bật sync để theo chủ phòng vào game
        BatGiaoDienDemNguoc();
        StartCoroutine(HienDemNguocMayKhach(System.Convert.ToDouble(vBatDau), System.Convert.ToSingle(vGiay)));
    }

    // =========================================================================
    // SỰ KIỆN TRONG PHÒNG
    // =========================================================================
    public override void OnJoinedRoom()
    {
        ChuanBiLaiPhongChoVangMoi();
        HienGiaoDienTrongPhong();

        if (PhotonNetwork.IsMasterClient)
        {
            GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        HienThongBaoTamThoi($"<color=#00FF00>{LayTen(newPlayer)} has joined the room!</color>");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (dangDem && !daGoiNapScene && KhongDuNguoiDeVaoGame())
        {
            HuyDemNguoc($"<color=#FFFF00>{LayTen(otherPlayer)} left. Not enough players (at least 2 required), countdown canceled!</color>");
            return;
        }

        HienThongBaoTamThoi($"<color=#FFFF00>{LayTen(otherPlayer)} has left the room!</color>");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient != null && newMasterClient.IsLocal && TiepQuanDemNguoc()) return;

        HienThongBaoTamThoi(newMasterClient.IsLocal
            ? "<color=#00FFFF>You are the new Room Host!</color>"
            : $"<color=#00FFFF>{LayTen(newMasterClient)} is the new Room Host!</color>");
    }

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
        statusText.text = $"Room creation error ({message}). Please try creating again!";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;
        HuyChoLenh();

        if (returnCode == ErrorCode.GameDoesNotExist)
            statusText.text = "Room not found. Please check the ROOM CODE!";
        else if (returnCode == ErrorCode.GameFull)
            statusText.text = "Room is full (maximum 4 players)!";
        else if (returnCode == ErrorCode.GameClosed)
            statusText.text = "The match in this room has already started!";
        else
            statusText.text = "Unable to join the room. Please try again!";
    }

    // =========================================================================
    // HIỂN THỊ TRẠNG THÁI PHÒNG
    // =========================================================================
    private string LayTen(Player p)
    {
        if (p == null) return "Player";
        return string.IsNullOrEmpty(p.NickName) ? $"Player #{p.ActorNumber}" : p.NickName;
    }

    private string TaoDanhSachNguoiChoi()
    {
        sb.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(LayTen(p));
            if (p.IsMasterClient) sb.Append(" (Host)");
            if (p.IsLocal) sb.Append(" (You)");
        }
        return sb.ToString();
    }

    private void HienTrangThaiPhong(string thongBao = null)
    {
        if (!PhotonNetwork.InRoom) return;
        if (dangDem) return;

        Room phong = PhotonNetwork.CurrentRoom;
        bool laChuPhong = PhotonNetwork.IsMasterClient;
        int tongNguoi = phong.PlayerCount;
        int soDangChonXe = DemNguoiDangChonXe(out string tenDangChon);
        int soSanSangOSanh = Mathf.Max(0, tongNguoi - soDangChonXe);
        bool conNguoiChonXe = soDangChonXe > 0;

        if (startButton != null)
        {
            startButton.SetActive(laChuPhong && !dangXuLy);
            if (startBtnCache == null) startBtnCache = startButton.GetComponent<Button>();
            if (startBtnCache != null) startBtnCache.interactable = !conNguoiChonXe;
        }

        string dauDong = string.IsNullOrEmpty(thongBao) ? "" : thongBao + "\n";
        string dongTrangThaiSan = $"In lobby (ready): <b>{soSanSangOSanh}/{tongNguoi}</b> | In {TEN_NUT_CHON_XE} (not {TEN_NUT_CONFIRM}): <b>{soDangChonXe}</b>";

        if (conNguoiChonXe)
            dongTrangThaiSan += $"\n<color=#FFD54F>Waiting for {tenDangChon} to click {TEN_NUT_CONFIRM} to return to lobby...</color>";

        string dongCuoi = laChuPhong
            ? (conNguoiChonXe
                ? $"Button {TEN_NUT_START} is locked — waiting for players in {TEN_NUT_CHON_XE} to click {TEN_NUT_CONFIRM} before starting!"
                : $"Everyone is in the lobby! Click {TEN_NUT_CHON_XE} to change vehicle / map, or click {TEN_NUT_START} to begin.")
            : $"Click {TEN_NUT_CHON_XE} to select a vehicle, then click {TEN_NUT_CONFIRM} to return to the lobby. Waiting for the Room Host to click {TEN_NUT_START}...";
        string danhSach = TaoDanhSachNguoiChoi();

        statusText.text =
            $"{dauDong}ROOM CODE: <color=#00FFFF><b>{phong.Name}</b></color> ({phong.PlayerCount}/{phong.MaxPlayers})\n" +
            $"Players: {danhSach}\n" +
            $"{dongTrangThaiSan}\n{dongCuoi}";
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
    private bool canDanMa;

    // Lấy mã phòng 5 ký tự từ chuỗi bất kỳ (người chơi có thể copy cả câu "MÃ PHÒNG: ABCDE (1/4)")
    private static string TachMaPhong(string raw)
    {
        string s = (raw ?? "").ToUpperInvariant();

        var m = System.Text.RegularExpressions.Regex.Match(s, "(?<![A-Z0-9])[A-HJ-NP-Z2-9]{5}(?![A-Z0-9])");
        if (m.Success) return m.Value;

        var sb = new StringBuilder();
        foreach (char c in s)
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);

        string t = sb.ToString();
        return t.Length > 5 ? t.Substring(0, 5) : t;
    }

    private static bool DangGiuCtrl()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);   // Mac
    }

    private void Update()
    {
        if (inputField == null || !inputField.isFocused) return;
        if (DangGiuCtrl() && Input.GetKeyDown(KeyCode.V)) canDanMa = true;
    }

    // Dán ở LateUpdate để đè lên kết quả dán mặc định của TMP (bị cắt cụt 5 ký tự đầu, có thể dính chữ thừa)
    private void LateUpdate()
    {
        if (!canDanMa) return;
        canDanMa = false;
        DanMa();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        DanMa();
    }

    private void DanMa()
    {
        if (inputField == null || !inputField.interactable) return;

        string ma = TachMaPhong(GUIUtility.systemCopyBuffer);
        if (string.IsNullOrEmpty(ma)) return;

        inputField.text = ma;
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }
}