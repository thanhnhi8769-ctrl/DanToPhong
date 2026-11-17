using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Thời gian")]
    public float thoiGianChoPhepVeDich = 30f;
    public float thoiGianHoiKhiQuaCheckPoint = 5f;
    public float thoiGianTruKhiChamNuoc = 5f;

    [Header("Phần thưởng & Mực nước")]
    public int thuongNguoiThang = 50;
    public int thuongChoiSolo = 10;
    public float mucNuocY = -0.3f;

    [Header("Luật thắng (nhiều người)")]
    public bool tuDongThangKhiConMotNguoi = true;

    [Header("Hiệu ứng đồng xu CheckPoint")]
    public float tocDoXoayDongXu = 120f;

    [Header("Camera xem trận")]
    public bool xemTheoCameraNguoiChoi = true;
    public float khoangCachSau = 7f;
    public float doCaoCamera = 3.5f;
    public float thoiGianXemNguoiThang = 5f;

    [Header("Trạng thái")]
    public bool ketThucGame = false;
    public bool winGame = false;
    public bool daBatDauDua = false;
    public bool daBiLoai = false;

    [Header("Giao diện UI")]
    public GameObject timeGameObject;
    public GameObject gameOverObject;
    public GameObject winGameObject;
    public GameObject resultPanelObject;

    [Header("Result Panel - kéo TextMeshPro vào đây (để trống sẽ tự tìm)")]
    public TMP_Text resultTitleText;
    public TMP_Text resultSubtitleText;
    public TMP_Text resultRewardText;
    public TMP_Text resultTotalCoinsText;
    public TMP_Text resultDetailText;
    public bool tuSapXepBoCucResult = true;
    public bool anChuThuaTrongResult = true;
    public bool dungResultPanelChoSolo = true;

    public GameObject eliminatePanelObject;
    public GameObject tutorialPanelObject;
    public GameObject pausePanelObject;
    public TMP_Text readyStatusText;
    public TMP_Text coinText;

    [Header("Panel khác (settings, shop...) - chỉ 1 panel hiện cùng lúc")]
    public GameObject[] panelKhac;

    [Header("HUD thời gian (thanh ngang tự vẽ)")]
    public bool dungHudThoiGianMoi = true;
    public GameObject thanhThoiGianCu;
    public Vector2 kichThuocThanh = new Vector2(560f, 52f);

    [Header("Âm thanh & Nhạc nền")]
    public AudioClip nhacNenDuaXe;
    public AudioClip amThanhCheckPoint;
    public AudioClip amThanhWin;
    public AudioClip amThanhGameOver;
    [Range(0f, 1f)] public float amLuongNhacNen = 0.4f;

    [Header("Tắt nhạc")]
    public KeyCode phimTatNhac = KeyCode.M;
    public bool taoNutNhacTuDong = true;
    public Vector2 viTriNutNhac = new Vector2(-360f, -20f);
    public float kichThuocNutNhac = 48f;

    [Header("Hiệu ứng nút (Màu trắng)")]
    public bool hieuUngNut = true;
    public Color mauVienNut = Color.white;

    private const string KEY_READY = "Ready";
    private const string KEY_ELIMINATED = "Eliminated";
    private const string KEY_ELIM_AT = "ElimAt";
    private const string KEY_SO_NGUOI = "SoNguoi";
    private const string KEY_NHAC_TAT = "NhacBiTat";

    private bool cheDoNhieuNguoi = false;

    private float thoiGianCoTheQuaLai = 0f;
    private float thoiGianCoTheBiTruNuoc = 0f;
    private float thoiDiemBatDauDua = -1f;
    private bool isPaused = false;
    private bool banThanDaBam = false;
    private bool daCoNguoiThang = false;
    private bool daDuyetThang = false;
    private bool daNhanThuongTranNay = false;
    private int soXuHienThiCuoi = -1;
    private int actorNguoiThang = -1;

    private bool coKetQua = false;
    private bool kqThang = false;
    private int kqXu = 0, kqTong = 0;
    private string kqPhuDe = "";

    private bool nhacBiTat = false;
    private GameObject nutNhac;
    private Image nutNhacIcon;
    private static Sprite spriteLoaBat, spriteLoaTat;

    private readonly List<GameObject> dsPanel = new List<GameObject>();
    private GameObject panelDangHien;

    private GameObject hudRoot;
    private TMP_Text hudSo;
    private Image hudFillImg;
    private RectTransform hudFillRect;
    private float hudToiDa = 30f;
    private float hudFillHienTai = 1f;
    private bool hudDaTao = false;
    private static Sprite spriteBoTron;
    private Image hudVien;
    private float hudFlashDen = 0f;

    private GameObject bannerRoot;
    private Image bannerVach;
    private TMP_Text bannerTieuDe;
    private TMP_Text bannerPhu;
    private Image phuMan;
    private Coroutine coroutinePhuMan;
    private Coroutine coroutineChucMung;

    private Vector3 viTriLucThua;
    private bool coViTriLucThua = false;
    private bool dangQuanhXe = false;
    private Camera camQuanh;
    private float gocQuanh = 0f;

    private GameObject[] danhSachDongXu;
    private Canvas canvasCache;
    private Camera mainCamCache;

    private TextMeshProUGUI timeTextCache;
    private int giayHienThiCuoi = -1;
    private XuLyVaChamChoXe xeCuaMinhCache;
    private float thoiDiemTimXeTiepTheo = 0f;

    private AudioSource nhacNenSource;
    private AudioSource hieuUngSource;
    private Coroutine coroutineSuKien;

    private bool dangXemTran = false;
    private bool dangChoKetQua = false;
    private Camera camXem;
    private Transform mucTieuXem;
    private float thoiDiemQuetXeTiepTheo = 0f;

    private GameObject thanhXem;
    private TMP_Text tenXemText;

    private class GocCam { public Vector3 pos; public Quaternion rot = Quaternion.identity; public float time; }
    private readonly Dictionary<int, GocCam> gocCamNguoiKhac = new Dictionary<int, GocCam>();
    private readonly GocCam camMacDinh = new GocCam();
    private bool coCamMacDinh = false;

    private float thoiDiemCapNhatXu = 0f;
    private float thoiDiemKiemTraPanel = 0f;
    private Transform scooterCuaMinh;
    private float thoiDiemTimScooter = 0f;

    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<GameManager>();
            return instance;
        }
    }

    // =========================================================================
    // KHỞI TẠO
    // =========================================================================
    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        Debug.developerConsoleVisible = false;
        Application.runInBackground = true;

        nhacBiTat = PlayerPrefs.GetInt(KEY_NHAC_TAT, 0) == 1;

        nhacNenSource = gameObject.AddComponent<AudioSource>();
        nhacNenSource.loop = true;
        nhacNenSource.playOnAwake = false;
        nhacNenSource.volume = amLuongNhacNen;
        nhacNenSource.mute = nhacBiTat;

        hieuUngSource = gameObject.AddComponent<AudioSource>();
        hieuUngSource.playOnAwake = false;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        instance = this;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (instance == this) instance = null;
    }

    private Camera LayMainCam()
    {
        if (mainCamCache == null) mainCamCache = Camera.main;
        return mainCamCache;
    }

    private Canvas LayCanvasChinh()
    {
        if (canvasCache == null)
        {
            canvasCache = (timeGameObject != null)
                ? timeGameObject.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();
        }
        return canvasCache;
    }

    private GameObject TimObjectTheoTenChinhXac(string ten)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name == ten && t.gameObject.scene == gameObject.scene)
                return t.gameObject;
        }
        return null;
    }

    private void CapNhatNoiDungTutorial()
    {
        if (tutorialPanelObject == null) return;

        TMP_Text content = null;
        TMP_Text title = null;

        foreach (TMP_Text t in tutorialPanelObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null) continue;

            if (t.gameObject.name.Equals("ContentText", System.StringComparison.OrdinalIgnoreCase))
                content = t;
            else if (title == null &&
                     (t.gameObject.name.Equals("Text (TMP)", System.StringComparison.OrdinalIgnoreCase) ||
                      t.gameObject.name.Equals("Title", System.StringComparison.OrdinalIgnoreCase)))
                title = t;
        }

        if (title != null) title.text = "HOW TO PLAY";
        if (content == null) return;

        content.text =
            "<b><color=#B8C0C8>CONTROLS</color></b>\n" +
            "W / ↑: Accelerate     S / ↓: Brake or reverse\n" +
            "A / ←: Turn left       D / →: Turn right       SPACE: Jump\n\n" +
            "<b><color=#B8C0C8>OBJECTIVE</color></b>\n" +
            "Avoid obstacles and reach the finish before time runs out.\n\n" +
            "<b><color=#B8C0C8>CHECKPOINT BONUS</color></b>\n" +
            "Pass a checkpoint: <color=#FFD633><b>+5 SECONDS</b></color>\n" +
            "Touch water: <color=#FF7777><b>-5 SECONDS</b></color> and return to the start.\n\n" +
            "<b><color=#B8C0C8>COINS & REWARDS</color></b>\n" +
            "Multiplayer win: <color=#FFD633><b>+50 COINS</b></color>   |   Solo win: <color=#FFD633><b>+10 COINS</b></color>\n" +
            "Spend coins in the SHOP to unlock 25 custom scooter colors.\n\n" +
            "<b><color=#B8C0C8>MULTIPLAYER</color></b>\n" +
            "Everyone must press START RACE before the race begins.";

        content.fontStyle = FontStyles.Normal;
        content.alignment = TextAlignmentOptions.TopLeft;
        content.textWrappingMode = TextWrappingModes.Normal;
        content.enableAutoSizing = true;
        content.fontSizeMin = 16f;
        content.fontSizeMax = 25f;
        content.paragraphSpacing = 5f;
    }

    private void DamBaoEventSystemChoTutorial()
    {
        EventSystem es = EventSystem.current ?? FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

        if (es == null)
        {
            GameObject esObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es = esObject.GetComponent<EventSystem>();
        }
        else if (!es.gameObject.activeInHierarchy)
        {
            es.gameObject.SetActive(true);
        }

        if (es != null)
        {
            BaseInputModule inputModule = es.GetComponent<BaseInputModule>();
            if (inputModule == null)
                inputModule = es.gameObject.AddComponent<StandaloneInputModule>();
            inputModule.enabled = true;
        }
    }

    private void DamBaoRaycasterChoPanel(GameObject panel)
    {
        if (panel == null) return;
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void KetNoiNutPausePanel()
    {
        if (pausePanelObject == null) return;

        foreach (Button button in pausePanelObject.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string key = button.gameObject.name.ToLowerInvariant();
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("resume") || key.Contains("continue") || key.Contains("back to game"))
            {
                button.onClick.RemoveListener(ResumeGame);
                button.onClick.AddListener(ResumeGame);
                button.interactable = true;
            }
            else if (key.Contains("main menu") || key.Contains("back to menu") ||
                     key.Contains("quit") || key.Contains("exit") || key.Contains("menu"))
            {
                button.onClick.RemoveListener(QuitGame);
                button.onClick.AddListener(QuitGame);
                button.interactable = true;
            }
        }
    }

    private void KetNoiNutResultPanel()
    {
        if (resultPanelObject == null) return;

        foreach (Button button in resultPanelObject.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string key = button.gameObject.name.ToLowerInvariant();
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("restart") || key.Contains("again") || key.Contains("retry"))
            {
                button.onClick.RemoveListener(RestartGame);
                button.onClick.AddListener(RestartGame);
                button.interactable = true;
            }
            else if (key.Contains("menu") || key.Contains("quit") || key.Contains("exit") || key.Contains("lobby"))
            {
                button.onClick.RemoveListener(QuitGame);
                button.onClick.AddListener(QuitGame);
                button.interactable = true;
            }
        }
    }

    private void KetNoiNutBatDauTutorial()
    {
        if (tutorialPanelObject == null) return;

        Button nutBatDau = null;
        int diemTotNhat = 0;

        foreach (Button b in tutorialPanelObject.GetComponentsInChildren<Button>(true))
        {
            if (b == null || b.gameObject.name == "NutXemThemHelp") continue;

            int diem = 0;
            string ten = b.gameObject.name.ToLowerInvariant();
            if (ten == "button") diem += 20;
            if (ten.Contains("start") || ten.Contains("begin")) diem += 30;

            foreach (TMP_Text label in b.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label == null) continue;
                string chu = label.text.ToUpperInvariant();
                if (chu.Contains("BẮT ĐẦU") || chu.Contains("START RACE") || chu.Contains("START GAME"))
                    diem += 100;
            }

            if (diem > diemTotNhat)
            {
                diemTotNhat = diem;
                nutBatDau = b;
            }
        }

        if (nutBatDau == null)
        {
            Transform buttonObject = tutorialPanelObject.transform.Find("Button");
            if (buttonObject != null)
            {
                Button btn = buttonObject.GetComponent<Button>();
                Image img = buttonObject.GetComponent<Image>();
                if (btn == null && img != null) btn = buttonObject.gameObject.AddComponent<Button>();
                if (btn != null)
                {
                    if (btn.targetGraphic == null) btn.targetGraphic = img;
                    nutBatDau = btn;
                }
            }
        }

        if (nutBatDau == null) return;

        nutBatDau.gameObject.SetActive(true);
        nutBatDau.enabled = true;
        nutBatDau.interactable = true;
        nutBatDau.onClick.RemoveListener(BatDauGame);
        nutBatDau.onClick.AddListener(BatDauGame);

        foreach (TMP_Text label in nutBatDau.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label != null)
            {
                label.text = "START RACE";
                label.raycastTarget = false;
            }
        }

        nutBatDau.transform.SetAsLastSibling();
    }

    private GameObject TimObjectTrongCanvas(string tuKhoa)
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return null;

        string key = tuKhoa.ToLower();
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            if (t.name.ToLower().Contains(key)) return t.gameObject;
        return null;
    }

    private TMP_Text TaoText(string ten, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                             float coChu, Color mau, TextAlignmentOptions canLe, bool vien)
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return null;

        GameObject obj = new GameObject(ten);
        obj.transform.SetParent(canvas.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = coChu;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = canLe;
        tmp.color = mau;
        tmp.raycastTarget = false;
        if (vien)
        {
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
        }
        return tmp;
    }

    private void XacDinhCheDo()
    {
        if (daBatDauDua) return;

        int soNguoi = 1;
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            soNguoi = PhotonNetwork.CurrentRoom.PlayerCount;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_SO_NGUOI, out object v))
                soNguoi = System.Convert.ToInt32(v);
        }
        cheDoNhieuNguoi = soNguoi > 1;
    }

    private void Start()
    {
        XacDinhCheDo();

        GameObject tutorialTrongScene = TimObjectTheoTenChinhXac("TutorialPanel");
        if (tutorialTrongScene != null) tutorialPanelObject = tutorialTrongScene;

        GameObject resultPanelTrongScene = TimObjectTheoTenChinhXac("ResultPanel");
        if (resultPanelTrongScene != null) resultPanelObject = resultPanelTrongScene;

        GameObject pausePanelTrongScene = TimObjectTheoTenChinhXac("PausePanel");
        if (pausePanelTrongScene != null) pausePanelObject = pausePanelTrongScene;

        GameObject eliminatePanelTrongScene = TimObjectTheoTenChinhXac("EliminatedPanel");
        if (eliminatePanelTrongScene != null) eliminatePanelObject = eliminatePanelTrongScene;

        if (timeGameObject == null)
            timeGameObject = TimObjectTheoTenChinhXac("time");

        CapNhatNoiDungTutorial();
        DamBaoEventSystemChoTutorial();
        DamBaoRaycasterChoPanel(pausePanelObject);
        KetNoiNutBatDauTutorial();
        KetNoiNutPausePanel();
        KetNoiNutResultPanel();

        danhSachDongXu = GameObject.FindGameObjectsWithTag("CheckPoint");

        if (eliminatePanelObject == null)
            eliminatePanelObject = TimObjectTrongCanvas("eliminate");

        if (resultPanelObject != null) resultPanelObject.SetActive(false);

        DangKyDanhSachPanel();

        if (timeGameObject != null) timeGameObject.SetActive(tutorialPanelObject == null);
        if (gameOverObject != null) gameOverObject.SetActive(false);
        if (winGameObject != null) winGameObject.SetActive(false);
        if (resultPanelObject != null) resultPanelObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        if (pausePanelObject != null) pausePanelObject.SetActive(false);

        if (readyStatusText == null)
        {
            GameObject objText = GameObject.Find("ReadyStatusText");
            if (objText != null) readyStatusText = objText.GetComponent<TMP_Text>();
        }
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);

        KhoiTaoHienThiXu();
        CapNhatHienThiXu(true);

        hudToiDa = Mathf.Max(1f, thoiGianChoPhepVeDich);
        if (dungHudThoiGianMoi) TaoHudThoiGian();
        if (taoNutNhacTuDong) TaoNutNhac();
        if (hieuUngNut) GanHieuUngChoMoiNut();

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { KEY_READY, false },
                { KEY_ELIMINATED, false },
                { KEY_ELIM_AT, null }
            });
        }

        HienThiThoiGianGame();

        if (tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(true);
            BatNhacNen();

            if (!cheDoNhieuNguoi) Time.timeScale = 0f;
            KiemTraSanSang();
        }
        else
        {
            BatDauDua();
        }
    }

    // =========================================================================
    // BẮT ĐẦU ĐUA & ĐỒNG BỘ SẴN SÀNG
    // =========================================================================
    public void BatDauGame()
    {
        if (daBatDauDua) return;

        if (!cheDoNhieuNguoi || !PhotonNetwork.InRoom)
        {
            BatDauDua();
            return;
        }

        if (banThanDaBam) return;
        banThanDaBam = true;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { KEY_READY, true }
        });

        KiemTraSanSang();
    }

    private void KiemTraSanSang()
    {
        if (daBatDauDua) return;

        if (!cheDoNhieuNguoi || !PhotonNetwork.InRoom)
        {
            if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
            return;
        }

        int tong = PhotonNetwork.CurrentRoom.PlayerCount;
        int sanSang = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(KEY_READY, out object v) && v is bool b && b)
                sanSang++;
        }

        if (readyStatusText != null)
        {
            readyStatusText.gameObject.SetActive(true);
            readyStatusText.text = banThanDaBam
                ? $"WAITING FOR PLAYERS... ({sanSang}/{tong})"
                : $"PRESS START RACE ({sanSang}/{tong} READY)";
        }

        if (sanSang >= tong)
        {
            BatDauDua();
        }
    }

    private void BatDauDua()
    {
        if (daBatDauDua) return;
        daBatDauDua = true;
        thoiDiemBatDauDua = Time.time;

        Time.timeScale = 1f;

        if (tutorialPanelObject != null) tutorialPanelObject.SetActive(false);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
        if (timeGameObject != null) timeGameObject.SetActive(true);

        BatNhacNen();
        HienThiThoiGianGame();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps == null) return;

        if (changedProps.ContainsKey(KEY_READY))
            KiemTraSanSang();

        if (changedProps.ContainsKey(KEY_ELIMINATED))
            KiemTraNguoiSotLaiCuoiCung();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!daBatDauDua)
        {
            XacDinhCheDo();
            KiemTraSanSang();
        }
        else
        {
            KiemTraNguoiSotLaiCuoiCung(true);
        }
    }

    private void BatNhacNen()
    {
        if (nhacNenSource == null || nhacNenDuaXe == null) return;
        if (nhacNenSource.clip != nhacNenDuaXe) nhacNenSource.clip = nhacNenDuaXe;
        nhacNenSource.mute = nhacBiTat;
        if (!nhacNenSource.isPlaying) nhacNenSource.Play();
    }

    // =========================================================================
    // CHỈ 1 PANEL HIỆN CÙNG LÚC
    // =========================================================================
    private void DangKyDanhSachPanel()
    {
        dsPanel.Clear();
        ThemPanel(pausePanelObject);
        ThemPanel(tutorialPanelObject);
        ThemPanel(winGameObject);
        ThemPanel(gameOverObject);
        ThemPanel(resultPanelObject);
        ThemPanel(eliminatePanelObject);

        if (panelKhac != null)
            foreach (GameObject p in panelKhac) ThemPanel(p);
    }

    private void ThemPanel(GameObject p)
    {
        if (p != null && !dsPanel.Contains(p)) dsPanel.Add(p);
    }

    private static bool LaChaConHoacNguocLai(GameObject a, GameObject b)
    {
        return a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform);
    }

    private void DamBaoMotPanel()
    {
        GameObject moiNhat = null;
        int dem = 0;

        for (int i = 0; i < dsPanel.Count; i++)
        {
            GameObject p = dsPanel[i];
            if (!DangMo(p)) continue;
            dem++;
            if (p != panelDangHien) moiNhat = p;
        }

        if (dem == 0) { panelDangHien = null; return; }

        if (dem == 1)
        {
            for (int i = 0; i < dsPanel.Count; i++)
                if (DangMo(dsPanel[i])) { panelDangHien = dsPanel[i]; break; }
            return;
        }

        if (moiNhat == null) moiNhat = panelDangHien;
        if (moiNhat == null) return;

        for (int i = 0; i < dsPanel.Count; i++)
        {
            GameObject p = dsPanel[i];
            if (p == moiNhat || !DangMo(p)) continue;
            if (LaChaConHoacNguocLai(p, moiNhat)) continue;
            p.SetActive(false);
        }

        panelDangHien = moiNhat;
        if (nutNhac != null) nutNhac.transform.SetAsLastSibling();
    }

    private void GanHieuUngNut(Button b)
    {
        if (!hieuUngNut || b == null) return;
        if (b.GetComponent<ButtonFx>() == null) b.gameObject.AddComponent<ButtonFx>();
    }

    private void GanHieuUngChoMoiNut()
    {
        ButtonFx.MauChung = Color.white;

        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tutorialPanelObject != null && b.transform.IsChildOf(tutorialPanelObject.transform))
                continue;
            GanHieuUngNut(b);
        }
    }

    // =========================================================================
    // TẮT / BẬT NHẠC
    // =========================================================================
    private static Sprite TaoSpriteLoa(bool bat)
    {
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = x - 36f, dy = y - 32f;
                bool on = false;

                if (x >= 10 && x <= 22 && y >= 24 && y <= 40) on = true;
                if (x > 22 && x <= 36 && Mathf.Abs(dy) <= 8f + (x - 22) * 12f / 14f) on = true;

                if (bat)
                {
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dx > 4f && Mathf.Abs(dy) <= dx &&
                        ((r >= 12f && r <= 15f) || (r >= 20f && r <= 23f))) on = true;
                }
                else
                {
                    float ux = x - 51f, uy = dy;
                    if (Mathf.Abs(ux) <= 8f && Mathf.Abs(uy) <= 8f &&
                        (Mathf.Abs(ux - uy) < 2.4f || Mathf.Abs(ux + uy) < 2.4f)) on = true;
                }

                tex.SetPixel(x, y, on ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
    }

    private void TaoNutNhac()
    {
        if (nutNhac != null) return;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        if (spriteLoaBat == null) spriteLoaBat = TaoSpriteLoa(true);
        if (spriteLoaTat == null) spriteLoaTat = TaoSpriteLoa(false);

        Image nen = TaoAnhCon("NutNhac", canvas.transform, new Color(0.22f, 0.25f, 0.30f, 0.92f));
        nen.raycastTarget = true;

        RectTransform r = nen.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = viTriNutNhac;
        r.sizeDelta = new Vector2(kichThuocNutNhac, kichThuocNutNhac);

        Button btn = nen.gameObject.AddComponent<Button>();
        btn.targetGraphic = nen;
        btn.onClick.AddListener(() =>
        {
            XoaTieuDiemUI();
            BatTatNhac();
        });

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(nen.transform, false);
        nutNhacIcon = iconObj.GetComponent<Image>();
        nutNhacIcon.raycastTarget = false;
        nutNhacIcon.preserveAspect = true;
        DatKhung(nutNhacIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        nutNhac = nen.gameObject;
        nutNhac.transform.SetAsLastSibling();
        CapNhatNutNhac();
    }

    private void CapNhatNutNhac()
    {
        if (nutNhacIcon == null) return;
        nutNhacIcon.sprite = nhacBiTat ? spriteLoaTat : spriteLoaBat;
        nutNhacIcon.color = nhacBiTat ? new Color(1f, 1f, 1f, 0.7f) : Color.white;
    }

    public void BatTatNhac()
    {
        nhacBiTat = !nhacBiTat;
        PlayerPrefs.SetInt(KEY_NHAC_TAT, nhacBiTat ? 1 : 0);
        if (nhacNenSource != null) nhacNenSource.mute = nhacBiTat;
        CapNhatNutNhac();
    }

    private static void XoaTieuDiemUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private static string LayTenNguoiChoi(Player p)
    {
        if (p == null) return "Player";
        return string.IsNullOrEmpty(p.NickName) ? $"Player #{p.ActorNumber}" : p.NickName;
    }

    private static bool DaBiLoai(Player p)
    {
        return p != null && p.CustomProperties.TryGetValue(KEY_ELIMINATED, out object v) && v is bool b && b;
    }

    // =========================================================================
    // XE CỦA MÌNH
    // =========================================================================
    private XuLyVaChamChoXe LayXeCuaMinh()
    {
        if (xeCuaMinhCache != null) return xeCuaMinhCache;
        if (Time.unscaledTime < thoiDiemTimXeTiepTheo) return null;
        thoiDiemTimXeTiepTheo = Time.unscaledTime + 0.5f;

        foreach (var xe in FindObjectsByType<XuLyVaChamChoXe>(FindObjectsSortMode.None))
        {
            if (!PhotonNetwork.IsConnected || xe.photonView == null || xe.photonView.IsMine)
            {
                xeCuaMinhCache = xe;
                break;
            }
        }
        return xeCuaMinhCache;
    }

    private Transform LayScooterCuaMinh()
    {
        if (scooterCuaMinh != null && scooterCuaMinh.gameObject.activeInHierarchy) return scooterCuaMinh;
        if (Time.unscaledTime < thoiDiemTimScooter) return null;
        thoiDiemTimScooter = Time.unscaledTime + 0.5f;

        XuLyVaChamChoXe xe = LayXeCuaMinh();
        if (xe != null) scooterCuaMinh = xe.transform;
        return scooterCuaMinh;
    }

    // =========================================================================
    // CAMERA XEM TRẬN
    // =========================================================================
    private void CapNhatCameraXemTran()
    {
        if (camXem == null) camXem = LayMainCam();
        if (camXem == null) return;

        if (!MultiplayerAllInOne.DangGoChat &&
            (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow) ||
             Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow)))
        {
            ChonXeXemTiepTheo();
        }

        if (mucTieuXem == null || !mucTieuXem.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= thoiDiemQuetXeTiepTheo)
            {
                thoiDiemQuetXeTiepTheo = Time.unscaledTime + 0.75f;
                ChonXeXemTiepTheo();
            }
            if (mucTieuXem == null)
            {
                if (coViTriLucThua) QuanhDiemChet(camXem);
                return;
            }
        }

        int actor = ActorCuaXe(mucTieuXem);
        GocCam g = null;
        bool coGocCam = false;

        if (xemTheoCameraNguoiChoi)
        {
            if (gocCamNguoiKhac.TryGetValue(actor, out g) && Time.unscaledTime - g.time < 2f)
                coGocCam = true;
            else if (coCamMacDinh)
            {
                g = camMacDinh;
                coGocCam = true;
            }
        }

        if (coGocCam)
        {
            Quaternion yaw = Quaternion.Euler(0f, mucTieuXem.eulerAngles.y, 0f);
            Vector3 viTriMong = mucTieuXem.position + yaw * g.pos;
            Quaternion xoayMong = yaw * g.rot;

            camXem.transform.position = Vector3.Lerp(camXem.transform.position, viTriMong, 12f * Time.unscaledDeltaTime);
            camXem.transform.rotation = Quaternion.Slerp(camXem.transform.rotation, xoayMong, 12f * Time.unscaledDeltaTime);
        }
        else
        {
            Vector3 viTriMong = mucTieuXem.position - mucTieuXem.forward * khoangCachSau + Vector3.up * doCaoCamera;
            camXem.transform.position = Vector3.Lerp(camXem.transform.position, viTriMong, 5f * Time.unscaledDeltaTime);

            Vector3 huongNhin = (mucTieuXem.position + Vector3.up) - camXem.transform.position;
            if (huongNhin.sqrMagnitude > 0.001f)
            {
                camXem.transform.rotation = Quaternion.Slerp(
                    camXem.transform.rotation,
                    Quaternion.LookRotation(huongNhin),
                    8f * Time.unscaledDeltaTime);
            }
        }
    }

    private IEnumerator XemNguoiThangRoiHienKetQua(string lyDo)
    {
        dangChoKetQua = true;
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        HienThongBaoSuKien(lyDo, Color.yellow, Mathf.Min(3f, thoiGianXemNguoiThang));

        ChonXeTheoActor(actorNguoiThang);
        CapNhatXemText();

        yield return new WaitForSecondsRealtime(thoiGianXemNguoiThang);

        dangChoKetQua = false;
        HienKetQuaThua(lyDo, true);
    }

    // =========================================================================
    // XU
    // =========================================================================
    private void KhoiTaoHienThiXu()
    {
        if (coinText != null) return;

        GameObject objCoSan = TimObjectTrongCanvas("cointext");
        if (objCoSan == null) objCoSan = TimObjectTrongCanvas("xutext");
        if (objCoSan != null)
        {
            coinText = objCoSan.GetComponent<TMP_Text>();
            if (coinText != null) return;
        }

        coinText = TaoText("TongXuHienTaiUI", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -25f), new Vector2(360f, 65f), 36, new Color(1f, 0.85f, 0.1f),
            TextAlignmentOptions.Left, true);
    }

    public void CapNhatHienThiXu(bool epCapNhat = false)
    {
        int tongXu = PlayerInventory.GetCoins();
        if (!epCapNhat && tongXu == soXuHienThiCuoi) return;
        soXuHienThiCuoi = tongXu;

        if (coinText != null)
        {
            coinText.gameObject.SetActive(!dangXemTran);
            coinText.text = $"COINS: {tongXu}";
        }

        CapNhatTextTongXuTrongPanel(resultPanelObject, tongXu);
        CapNhatTextTongXuTrongPanel(winGameObject, tongXu);
        CapNhatTextTongXuTrongPanel(gameOverObject, tongXu);
        CapNhatTextTongXuTrongPanel(pausePanelObject, tongXu);
    }

    private static bool LaTextTongXu(string tenObj)
    {
        return tenObj.Contains("total") || tenObj.Contains("tong") || tenObj.Contains("balance");
    }

    private void CapNhatTextTongXuTrongPanel(GameObject panel, int tongXu)
    {
        if (panel == null) return;
        foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t != null && LaTextTongXu(t.gameObject.name.ToLowerInvariant()))
                t.text = $"TOTAL COINS: {tongXu}";
        }
    }

    // =========================================================================
    // BANNER & HIỆU ỨNG MÀN HÌNH
    // =========================================================================
    private void HienThongBaoSuKien(string noiDung, Color mau, float thoiGian = 2f)
    {
        if (CoBangDangMo()) return;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        if (bannerRoot == null)
        {
            bannerRoot = new GameObject("SuKienBanner", typeof(RectTransform));
            bannerRoot.transform.SetParent(canvas.transform, false);
            RectTransform rr = (RectTransform)bannerRoot.transform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.78f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = Vector2.zero;
            rr.sizeDelta = new Vector2(760f, 92f);

            Image nen = TaoAnhCon("Nen", bannerRoot.transform, new Color(0.05f, 0.08f, 0.12f, 0.88f));
            Stretch(nen.rectTransform);

            bannerVach = TaoAnhCon("Vach", bannerRoot.transform, mau);
            RectTransform rv = bannerVach.rectTransform;
            rv.anchorMin = new Vector2(0f, 0f);
            rv.anchorMax = new Vector2(1f, 0f);
            rv.pivot = new Vector2(0.5f, 0f);
            rv.anchoredPosition = Vector2.zero;
            rv.sizeDelta = new Vector2(0f, 5f);

            bannerTieuDe = TaoChuCon("TieuDe", bannerRoot.transform, 32f, Color.white, true);
            Stretch(bannerTieuDe.rectTransform);

            bannerPhu = TaoChuCon("Phu", bannerRoot.transform, 18f, Color.white, false);
            bannerPhu.gameObject.SetActive(false);
        }

        bannerRoot.SetActive(true);
        bannerVach.color = mau;
        bannerTieuDe.text = noiDung;
        bannerTieuDe.color = mau;

        if (coroutineSuKien != null) StopCoroutine(coroutineSuKien);
        coroutineSuKien = StartCoroutine(AnBannerSau(thoiGian));
    }

    private IEnumerator AnBannerSau(float giay)
    {
        yield return new WaitForSecondsRealtime(giay);
        if (bannerRoot != null) bannerRoot.SetActive(false);
        coroutineSuKien = null;
    }

    private void HienPopupThoiGian(string noiDung, Color mau)
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        TMP_Text pop = TaoChuCon("PopupThoiGian", canvas.transform, 34f, mau, true);
        RectTransform r = pop.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -78f);
        r.sizeDelta = new Vector2(260f, 50f);
        pop.text = noiDung;

        StartCoroutine(ChayPopupThoiGian(pop));
    }

    private IEnumerator ChayPopupThoiGian(TMP_Text pop)
    {
        RectTransform r = pop.rectTransform;
        Vector2 start = r.anchoredPosition;
        const float tong = 0.85f;

        for (float t = 0f; t < tong; t += Time.unscaledDeltaTime)
        {
            if (pop == null) yield break;
            float k = t / tong;
            r.anchoredPosition = start + new Vector2(0f, -24f * k);
            Color c = pop.color;
            c.a = 1f - k;
            pop.color = c;
            yield return null;
        }

        if (pop != null) Destroy(pop.gameObject);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void DatKhung(RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.offsetMin = oMin;
        r.offsetMax = oMax;
    }

    private void HienPhuMan(Color mau)
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        if (phuMan == null)
        {
            GameObject o = new GameObject("PhuManKetQua", typeof(RectTransform), typeof(Image));
            o.transform.SetParent(canvas.transform, false);
            phuMan = o.GetComponent<Image>();
            phuMan.raycastTarget = false;
            Stretch(phuMan.rectTransform);
        }

        phuMan.transform.SetAsFirstSibling();
        phuMan.gameObject.SetActive(true);

        if (coroutinePhuMan != null) StopCoroutine(coroutinePhuMan);
        coroutinePhuMan = StartCoroutine(ChayPhuMan(mau));
    }

    private IEnumerator ChayPhuMan(Color mau)
    {
        const float tong = 0.8f;
        for (float t = 0f; t < tong; t += Time.unscaledDeltaTime)
        {
            Color c = mau;
            c.a = Mathf.Lerp(0f, mau.a, t / tong);
            phuMan.color = c;
            yield return null;
        }
        phuMan.color = mau;
    }

    private void BatDauCameraQuanhXe()
    {
        if (!coViTriLucThua || dangQuanhXe) return;

        camQuanh = LayMainCam() != null ? LayMainCam() : FindFirstObjectByType<Camera>();
        if (camQuanh == null) return;

        foreach (MonoBehaviour mb in camQuanh.GetComponents<MonoBehaviour>())
            mb.enabled = false;

        Vector3 off = camQuanh.transform.position - viTriLucThua;
        off.y = 0f;
        gocQuanh = off.sqrMagnitude > 0.01f ? Mathf.Atan2(off.x, off.z) * Mathf.Rad2Deg : 0f;
        dangQuanhXe = true;
    }

    private void QuanhDiemChet(Camera cam)
    {
        gocQuanh += 18f * Time.unscaledDeltaTime;

        Vector3 tam = viTriLucThua + Vector3.up * 1f;
        Vector3 viTriMong = tam + Quaternion.Euler(0f, gocQuanh, 0f) * (Vector3.forward * 9f) + Vector3.up * 3.5f;

        cam.transform.position = Vector3.Lerp(cam.transform.position, viTriMong, 3f * Time.unscaledDeltaTime);

        Vector3 huongNhin = tam - cam.transform.position;
        if (huongNhin.sqrMagnitude > 0.001f)
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(huongNhin), 5f * Time.unscaledDeltaTime);
    }

    private static void MoKhoaChuot()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static bool DangMo(GameObject g)
    {
        return g != null && g.activeInHierarchy;
    }

    private bool CoBangDangMo()
    {
        return DangMo(winGameObject) || DangMo(gameOverObject) || DangMo(resultPanelObject)
            || DangMo(eliminatePanelObject) || DangMo(pausePanelObject) || DangMo(tutorialPanelObject);
    }

    private void AnBanner()
    {
        if (coroutineSuKien != null) { StopCoroutine(coroutineSuKien); coroutineSuKien = null; }
        if (bannerRoot != null) bannerRoot.SetActive(false);
    }

    private void TaoChuThuongTrongBang(GameObject bang, int xuNhanDuoc)
    {
        if (bang == null) return;

        TMP_Text t = TaoChuCon("RewardTextTuDong", bang.transform, 46f, new Color(1f, 0.85f, 0.2f), true);
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.3f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(600f, 70f);
        t.text = $"+{xuNhanDuoc} COINS";
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    private void Update()
    {
        if (!MultiplayerAllInOne.DangGoChat &&
            !daBatDauDua && tutorialPanelObject != null &&
            tutorialPanelObject.activeInHierarchy &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            BatDauGame();
        }

        if (!ketThucGame && !winGame && resultPanelObject != null && resultPanelObject.activeSelf)
        {
            resultPanelObject.SetActive(false);
            if (panelDangHien == resultPanelObject) panelDangHien = null;
        }

        float unscaledNow = Time.unscaledTime;

        if (unscaledNow >= thoiDiemCapNhatXu)
        {
            thoiDiemCapNhatXu = unscaledNow + 0.3f;
            CapNhatHienThiXu();
        }

        CapNhatHudThoiGian();

        if (unscaledNow >= thoiDiemKiemTraPanel)
        {
            thoiDiemKiemTraPanel = unscaledNow + 0.12f;
            DamBaoMotPanel();
        }

        if (!MultiplayerAllInOne.DangGoChat && Input.GetKeyDown(phimTatNhac))
            BatTatNhac();

        if (bannerRoot != null && bannerRoot.activeSelf && CoBangDangMo()) AnBanner();

        if (danhSachDongXu != null)
        {
            Camera cam = LayMainCam();
            if (cam != null)
            {
                float goc = tocDoXoayDongXu * Time.deltaTime;
                Vector3 camPos = cam.transform.position;
                for (int i = 0; i < danhSachDongXu.Length; i++)
                {
                    GameObject xu = danhSachDongXu[i];
                    if (xu != null && xu.activeInHierarchy && (xu.transform.position - camPos).sqrMagnitude < 3600f)
                        xu.transform.Rotate(0f, goc, 0f, Space.World);
                }
            }
        }

        if (!MultiplayerAllInOne.DangGoChat && Input.GetKeyDown(KeyCode.Escape))
            XuLyEscNgoaiTranDua();

        if (dangQuanhXe && camQuanh != null)
        {
            QuanhDiemChet(camQuanh);
            return;
        }

        if (dangXemTran)
        {
            CapNhatCameraXemTran();
            return;
        }

        bool dangDua = daBatDauDua && !daBiLoai && !ketThucGame && !winGame;

        if (dangDua && !isPaused)
        {
            thoiGianChoPhepVeDich -= Time.deltaTime;
            if (thoiGianChoPhepVeDich <= 0f)
            {
                thoiGianChoPhepVeDich = 0f;
                HienThiThoiGianGame();
                XuLyBanThanBiLoai();
                return;
            }
            HienThiThoiGianGame();
        }
    }

    // =========================================================================
    // SPRITE TỰ VẼ
    // =========================================================================
    private static Sprite LaySpriteBoTron()
    {
        if (spriteBoTron != null) return spriteBoTron;

        const int kichThuoc = 32;
        const float bk = 10f;
        Texture2D tex = new Texture2D(kichThuoc, kichThuoc, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < kichThuoc; y++)
        {
            for (int x = 0; x < kichThuoc; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, bk, kichThuoc - bk);
                float cy = Mathf.Clamp(py, bk, kichThuoc - bk);
                float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(bk - d + 0.5f)));
            }
        }
        tex.Apply();

        spriteBoTron = Sprite.Create(tex, new Rect(0, 0, kichThuoc, kichThuoc), new Vector2(0.5f, 0.5f),
                                     100f, 0, SpriteMeshType.FullRect, new Vector4(bk, bk, bk, bk));
        return spriteBoTron;
    }

    private static Image TaoAnhCon(string ten, Transform cha, Color mau)
    {
        GameObject o = new GameObject(ten, typeof(RectTransform), typeof(Image));
        o.transform.SetParent(cha, false);
        Image img = o.GetComponent<Image>();
        img.sprite = LaySpriteBoTron();
        img.type = Image.Type.Sliced;
        img.color = mau;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text TaoChuCon(string ten, Transform cha, float coChu, Color mau, bool vien)
    {
        GameObject o = new GameObject(ten, typeof(RectTransform));
        o.transform.SetParent(cha, false);
        TextMeshProUGUI tmp = o.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = coChu;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = mau;
        tmp.raycastTarget = false;
        if (vien)
        {
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
        }
        return tmp;
    }

    // =========================================================================
    // HUD THỜI GIAN (THANH NGANG)
    // =========================================================================
    private void AnThanhCu(GameObject go)
    {
        if (go == null) return;

        if (timeGameObject != null && timeGameObject.transform.IsChildOf(go.transform))
        {
            foreach (Image img in go.GetComponentsInChildren<Image>(true))
                if (!img.transform.IsChildOf(timeGameObject.transform)) img.enabled = false;
        }
        else
        {
            go.SetActive(false);
        }
    }

    private void TaoHudThoiGian()
    {
        if (hudDaTao) return;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        if (thanhThoiGianCu != null) AnThanhCu(thanhThoiGianCu);
        else
        {
            foreach (Slider s in canvas.GetComponentsInChildren<Slider>(true))
            {
                string ten = s.gameObject.name.ToLower();
                if (ten.Contains("time") || ten.Contains("thoi")) AnThanhCu(s.gameObject);
            }
        }

        float w = kichThuocThanh.x, h = kichThuocThanh.y;
        float bw = h * 2.2f;

        hudRoot = new GameObject("HudThoiGian", typeof(RectTransform));
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform rr = (RectTransform)hudRoot.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f);
        rr.pivot = new Vector2(0.5f, 1f);
        rr.anchoredPosition = new Vector2(0f, -16f);
        rr.sizeDelta = new Vector2(w, h);

        hudVien = TaoAnhCon("Quang", hudRoot.transform, new Color(0.3f, 0.9f, 0.45f, 0.35f));
        DatKhung(hudVien.rectTransform, Vector2.zero, Vector2.one, new Vector2(-6f, -6f), new Vector2(6f, 6f));

        Image nen = TaoAnhCon("Nen", hudRoot.transform, new Color(0.04f, 0.07f, 0.12f, 0.94f));
        Stretch(nen.rectTransform);

        Image ray = TaoAnhCon("Ray", hudRoot.transform, new Color(1f, 1f, 1f, 0.12f));
        DatKhung(ray.rectTransform, Vector2.zero, Vector2.one,
                 new Vector2(12f, h * 0.27f), new Vector2(-(bw + 8f), -h * 0.27f));

        hudFillImg = TaoAnhCon("Fill", ray.transform, new Color(0.3f, 0.9f, 0.45f, 1f));
        hudFillRect = hudFillImg.rectTransform;
        Stretch(hudFillRect);

        Image gloss = TaoAnhCon("Bong", hudFillImg.transform, new Color(1f, 1f, 1f, 0.22f));
        DatKhung(gloss.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(5f, 0f), new Vector2(-5f, -2f));

        Image badge = TaoAnhCon("Badge", hudRoot.transform, new Color(1f, 1f, 1f, 0.08f));
        RectTransform rb = badge.rectTransform;
        rb.anchorMin = new Vector2(1f, 0f);
        rb.anchorMax = new Vector2(1f, 1f);
        rb.offsetMin = new Vector2(-bw - 6f, 6f);
        rb.offsetMax = new Vector2(-6f, -6f);

        hudSo = TaoChuCon("So", badge.transform, h * 0.6f, Color.white, true);
        hudSo.enableAutoSizing = true;
        hudSo.fontSizeMin = 14f;
        hudSo.fontSizeMax = h * 0.6f;
        hudSo.textWrappingMode = TextWrappingModes.NoWrap;
        DatKhung(hudSo.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));

        hudFillHienTai = 1f;
        hudDaTao = true;
        hudRoot.SetActive(timeGameObject == null || timeGameObject.activeInHierarchy);
    }

    private void CapNhatHudThoiGian()
    {
        if (!hudDaTao || hudRoot == null) return;

        bool hien = timeGameObject == null || timeGameObject.activeInHierarchy;
        if (hudRoot.activeSelf != hien) hudRoot.SetActive(hien);
        if (!hien) return;

        if (timeTextCache == null && timeGameObject != null)
            timeTextCache = timeGameObject.GetComponent<TextMeshProUGUI>();
        if (timeTextCache != null && timeTextCache.enabled) timeTextCache.enabled = false;

        float t = Mathf.Max(0f, thoiGianChoPhepVeDich);
        if (t > hudToiDa) hudToiDa = t;
        float tiLe = hudToiDa > 0f ? Mathf.Clamp01(t / hudToiDa) : 0f;

        hudFillHienTai = Mathf.Lerp(hudFillHienTai, tiLe, 10f * Time.unscaledDeltaTime);

        Color mau = tiLe > 0.5f ? new Color(0.3f, 0.9f, 0.45f)
                  : tiLe > 0.25f ? new Color(1f, 0.78f, 0.2f)
                  : new Color(1f, 0.3f, 0.3f);

        if (hudFillImg != null)
        {
            hudFillRect.anchorMax = new Vector2(Mathf.Clamp01(hudFillHienTai), 1f);
            hudFillImg.color = mau;
            hudFillImg.enabled = hudFillHienTai > 0.01f;
        }

        bool dangNhay = Time.unscaledTime < hudFlashDen;
        bool sapHet = t <= 5f;

        if (hudVien != null)
        {
            Color mv = dangNhay ? new Color(1f, 0.85f, 0.25f) : mau;
            mv.a = dangNhay ? 0.7f : (sapHet ? 0.2f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 0.3f);
            hudVien.color = mv;
        }

        if (hudSo != null)
        {
            hudSo.text = $"{Mathf.CeilToInt(t)}<size=55%>s</size>";
            hudSo.color = dangNhay ? new Color(1f, 0.9f, 0.35f) : (sapHet ? mau : Color.white);

            float xung = 1f;
            if (dangNhay) xung = 1f + 0.14f * Mathf.Sin((hudFlashDen - Time.unscaledTime) * 18f);
            else if (sapHet) xung = 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            hudSo.transform.localScale = Vector3.one * xung;
        }
    }

    public void HienThiThoiGianGame()
    {
        int giay = Mathf.CeilToInt(Mathf.Max(0f, thoiGianChoPhepVeDich));
        if (timeTextCache == null && timeGameObject != null)
            timeTextCache = timeGameObject.GetComponentInChildren<TextMeshProUGUI>(true);

        if (timeTextCache != null && giay != giayHienThiCuoi)
        {
            giayHienThiCuoi = giay;
            timeTextCache.text = $"{giay}s";
        }
    }

    // =========================================================================
    // CHECKPOINT, NƯỚC & XỬ LÝ THẮNG / THUA
    // =========================================================================
    public void QuaCheckPoint()
    {
        if (!daBatDauDua || ketThucGame || winGame || daBiLoai) return;
        if (Time.time < thoiGianCoTheQuaLai) return;

        thoiGianCoTheQuaLai = Time.time + 0.5f;
        thoiGianChoPhepVeDich += thoiGianHoiKhiQuaCheckPoint;
        if (thoiGianChoPhepVeDich > hudToiDa) hudToiDa = thoiGianChoPhepVeDich;
        hudFlashDen = Time.unscaledTime + 0.45f;

        if (!nhacBiTat && hieuUngSource != null && amThanhCheckPoint != null)
            hieuUngSource.PlayOneShot(amThanhCheckPoint);

        HienPopupThoiGian($"+{Mathf.RoundToInt(thoiGianHoiKhiQuaCheckPoint)}s", new Color(1f, 0.88f, 0.25f));
        HienThiThoiGianGame();
    }

    public void QuaCheckPoint(Collider other)
    {
        QuaCheckPoint();
    }

    public void ChamNuoc()
    {
        if (!daBatDauDua || ketThucGame || winGame || daBiLoai) return;
        if (Time.time < thoiGianCoTheBiTruNuoc) return;

        thoiGianCoTheBiTruNuoc = Time.time + 1.5f;
        thoiGianChoPhepVeDich = Mathf.Max(0f, thoiGianChoPhepVeDich - thoiGianTruKhiChamNuoc);

        HienPopupThoiGian($"-{Mathf.RoundToInt(thoiGianTruKhiChamNuoc)}s", new Color(1f, 0.35f, 0.35f));
        HienThiThoiGianGame();

        if (thoiGianChoPhepVeDich <= 0f)
            XuLyBanThanBiLoai();
    }

    public void QuaWinPoint(Collider other = null)
    {
        VeDichThanhCong();
    }

    public void VeDichThanhCong()
    {
        if (ketThucGame || winGame || daBiLoai || daCoNguoiThang) return;

        daCoNguoiThang = true;
        winGame = true;
        ketThucGame = true;

        int xuNhan = cheDoNhieuNguoi ? thuongNguoiThang : thuongChoiSolo;
        if (!daNhanThuongTranNay && xuNhan > 0)
        {
            daNhanThuongTranNay = true;
            PlayerInventory.AddCoins(xuNhan);
            CapNhatHienThiXu(true);
        }

        if (!nhacBiTat && hieuUngSource != null && amThanhWin != null)
            hieuUngSource.PlayOneShot(amThanhWin);

        MatchHistorySystem.LuuKetQuaTran(true, xuNhan);

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RPC_CoNguoiVeDich), RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // Phân biệt câu chúc hợp lý cho chế độ 1 người vs nhiều người
        string loiChucPhuDe = cheDoNhieuNguoi
            ? "YOU REACHED THE FINISH LINE FIRST!"
            : "YOU CROSSED THE FINISH LINE!";

        HienBangKetQuaCuoi(true, xuNhan, loiChucPhuDe);
    }

    [PunRPC]
    private void RPC_CoNguoiVeDich(int winnerActor)
    {
        if (ketThucGame || winGame) return;

        daCoNguoiThang = true;
        actorNguoiThang = winnerActor;

        string tenThang = "ANOTHER PLAYER";
        if (PhotonNetwork.InRoom)
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == winnerActor)
                {
                    tenThang = LayTenNguoiChoi(p).ToUpper();
                    break;
                }
            }
        }

        string lyDo = $"{tenThang} FINISHED FIRST!";
        if (dangXemTran && !dangChoKetQua)
            StartCoroutine(XemNguoiThangRoiHienKetQua(lyDo));
        else
            HienKetQuaThua(lyDo, true);
    }

    private void XuLyBanThanBiLoai()
    {
        if (daBiLoai || ketThucGame || winGame) return;
        daBiLoai = true;

        Transform xe = LayScooterCuaMinh();
        if (xe != null)
        {
            viTriLucThua = xe.position;
            coViTriLucThua = true;
        }

        if (!nhacBiTat && hieuUngSource != null && amThanhGameOver != null)
            hieuUngSource.PlayOneShot(amThanhGameOver);

        MatchHistorySystem.LuuKetQuaTran(false, 0);

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { KEY_ELIMINATED, true },
                { KEY_ELIM_AT, PhotonNetwork.Time }
            });

            if (eliminatePanelObject != null)
                eliminatePanelObject.SetActive(true);

            BatDauCheDoXemTran();
            KiemTraNguoiSotLaiCuoiCung();
            return;
        }

        HienKetQuaThua("TIME'S UP!", false);
    }

    private void KiemTraNguoiSotLaiCuoiCung(bool doThoatPhong = false)
    {
        if (!cheDoNhieuNguoi || !tuDongThangKhiConMotNguoi || daDuyetThang || ketThucGame || winGame || daCoNguoiThang)
            return;

        if (!PhotonNetwork.InRoom) return;
        if (doThoatPhong && PhotonNetwork.CurrentRoom.PlayerCount <= 1) return;

        int conSong = 0;
        Player nguoiConSong = null;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!DaBiLoai(p))
            {
                conSong++;
                nguoiConSong = p;
            }
        }

        if (conSong == 1 && nguoiConSong != null)
        {
            daDuyetThang = true;
            if (nguoiConSong.IsLocal && !daBiLoai)
                VeDichThanhCong();
        }
    }

    private void HienKetQuaThua(string lyDo, bool tatXemTran)
    {
        if (tatXemTran)
        {
            dangXemTran = false;
            if (thanhXem != null) thanhXem.SetActive(false);
        }

        ketThucGame = true;
        winGame = false;

        if (coViTriLucThua)
            BatDauCameraQuanhXe();

        HienBangKetQuaCuoi(false, 0, lyDo);
    }

    private void HienBangKetQuaCuoi(bool thang, int xuThuong, string phuDe)
    {
        coKetQua = true;
        kqThang = thang;
        kqXu = xuThuong;
        kqTong = PlayerInventory.GetCoins();
        kqPhuDe = phuDe;

        AnBanner();
        if (hudRoot != null) hudRoot.SetActive(false);
        if (timeGameObject != null) timeGameObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        if (thanhXem != null) thanhXem.SetActive(false);

        HienPhuMan(thang ? new Color(0.05f, 0.25f, 0.14f, 0.55f) : new Color(0.28f, 0.05f, 0.05f, 0.55f));
        MoKhoaChuot();

        if (!cheDoNhieuNguoi && !dungResultPanelChoSolo)
        {
            GameObject bang = thang ? winGameObject : gameOverObject;
            if (bang != null)
            {
                bang.SetActive(true);
                if (thang && xuThuong > 0) TaoChuThuongTrongBang(bang, xuThuong);
                return;
            }
        }

        if (resultPanelObject != null)
        {
            resultPanelObject.SetActive(true);
            CapNhatNoiDungResultPanel();
        }
        else
        {
            GameObject bang = thang ? winGameObject : gameOverObject;
            if (bang != null) bang.SetActive(true);
        }
    }

    private static void CanChinhDongTextResult(TMP_Text txt, float posY, float width, float height)
    {
        if (txt == null) return;
        RectTransform r = txt.rectTransform;
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0f, posY);
        r.sizeDelta = new Vector2(width, height);
        txt.alignment = TextAlignmentOptions.Center;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void CapNhatNoiDungResultPanel()
    {
        if (!coKetQua || resultPanelObject == null) return;

        foreach (TMP_Text t in resultPanelObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null) continue;
            string n = t.gameObject.name.ToLowerInvariant();
            if (resultTitleText == null && n.Contains("title")) resultTitleText = t;
            else if (resultSubtitleText == null && n.Contains("sub")) resultSubtitleText = t;
            else if (resultRewardText == null && n.Contains("reward")) resultRewardText = t;
            else if (resultTotalCoinsText == null && (n.Contains("total") || n.Contains("balance"))) resultTotalCoinsText = t;
            else if (anChuThuaTrongResult && t.GetComponentInParent<Button>() == null &&
                     (t.text.Trim().Equals("NEW TEXT", System.StringComparison.OrdinalIgnoreCase) || n.Contains("detail")))
                t.gameObject.SetActive(false);
        }

        // Tự động sắp xếp lại bố cục dọc gọn gàng, tránh đè chữ TOTAL COINS lên tiêu đề
        if (tuSapXepBoCucResult)
        {
            CanChinhDongTextResult(resultTitleText, 145f, 900f, 80f);
            CanChinhDongTextResult(resultSubtitleText, 75f, 900f, 55f);
            CanChinhDongTextResult(resultRewardText, 10f, 700f, 50f);
            CanChinhDongTextResult(resultTotalCoinsText, -48f, 700f, 46f);
        }

        if (resultTitleText != null)
        {
            if (kqThang)
                resultTitleText.text = cheDoNhieuNguoi ? "CONGRATULATIONS!" : "RACE COMPLETED!";
            else
                resultTitleText.text = "GAME OVER";

            resultTitleText.color = kqThang ? new Color(1f, 0.85f, 0.15f) : new Color(1f, 0.35f, 0.35f);
            resultTitleText.enableAutoSizing = true;
            resultTitleText.fontSizeMin = 28f;
            resultTitleText.fontSizeMax = 62f;
        }

        if (resultSubtitleText != null)
        {
            resultSubtitleText.text = kqPhuDe;
            resultSubtitleText.color = Color.white;
        }

        if (resultRewardText != null)
        {
            resultRewardText.text = $"REWARD: +{kqXu} COINS";
            resultRewardText.color = Color.white;
        }

        if (resultTotalCoinsText != null)
        {
            resultTotalCoinsText.text = $"TOTAL COINS: {kqTong}";
            resultTotalCoinsText.color = new Color(1f, 0.88f, 0.25f);
        }

        if (kqThang)
        {
            if (coroutineChucMung != null) StopCoroutine(coroutineChucMung);
            coroutineChucMung = StartCoroutine(ChayHieuUngChucMung());
        }
    }

    private IEnumerator ChayHieuUngChucMung()
    {
        if (resultPanelObject == null) yield break;

        Color[] bangMau =
        {
            new Color(1f, 0.85f, 0.15f),
            new Color(0.25f, 0.95f, 0.55f),
            new Color(0.3f, 0.75f, 1f),
            new Color(1f, 0.4f, 0.65f),
            new Color(1f, 0.55f, 0.15f)
        };

        RectTransform panelRect = resultPanelObject.GetComponent<RectTransform>();
        float rong = panelRect != null && panelRect.rect.width > 100f ? panelRect.rect.width : 1200f;
        float cao = panelRect != null && panelRect.rect.height > 100f ? panelRect.rect.height : 700f;

        List<RectTransform> manhPhao = new List<RectTransform>(45);
        List<Vector2> vanToc = new List<Vector2>(45);
        List<float> tocDoXoay = new List<float>(45);

        for (int i = 0; i < 45; i++)
        {
            GameObject go = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(resultPanelObject.transform, false);
            Image img = go.GetComponent<Image>();
            img.color = bangMau[i % bangMau.Length];
            img.raycastTarget = false;

            RectTransform r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(Random.Range(-rong * 0.45f, rong * 0.45f), Random.Range(0f, cao * 0.45f));
            r.sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(6f, 12f));

            manhPhao.Add(r);
            vanToc.Add(new Vector2(Random.Range(-70f, 70f), Random.Range(-180f, -90f)));
            tocDoXoay.Add(Random.Range(-360f, 360f));
        }

        float t = 0f;
        while (resultPanelObject != null && resultPanelObject.activeInHierarchy)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            if (resultTitleText != null)
            {
                float scale = 1f + 0.06f * Mathf.Sin(t * 5f);
                resultTitleText.transform.localScale = Vector3.one * scale;
                resultTitleText.color = Color.Lerp(
                    new Color(1f, 0.85f, 0.15f),
                    Color.white,
                    0.35f + 0.35f * Mathf.Sin(t * 4f)
                );
            }

            for (int i = 0; i < manhPhao.Count; i++)
            {
                RectTransform r = manhPhao[i];
                if (r == null) continue;

                Vector2 pos = r.anchoredPosition + vanToc[i] * dt;
                pos.x += Mathf.Sin(t * 3f + i) * 35f * dt;
                if (pos.y < -cao * 0.5f)
                {
                    pos.y = cao * 0.5f;
                    pos.x = Random.Range(-rong * 0.45f, rong * 0.45f);
                }

                r.anchoredPosition = pos;
                r.Rotate(0f, 0f, tocDoXoay[i] * dt);
            }

            yield return null;
        }
    }

    // =========================================================================
    // SPECTATOR (XEM TRẬN)
    // =========================================================================
    private void BatDauCheDoXemTran()
    {
        dangXemTran = true;
        camXem = LayMainCam();
        if (coinText != null) coinText.gameObject.SetActive(false);

        if (thanhXem == null)
        {
            Canvas canvas = LayCanvasChinh();
            if (canvas != null)
            {
                Image nen = TaoAnhCon("ThanhXemTran", canvas.transform, new Color(0.08f, 0.09f, 0.11f, 0.9f));
                RectTransform r = nen.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 28f);
                r.sizeDelta = new Vector2(460f, 54f);
                thanhXem = nen.gameObject;

                tenXemText = TaoChuCon("TenXem", thanhXem.transform, 22f, Color.white, true);
                Stretch(tenXemText.rectTransform);
            }
        }

        if (thanhXem != null) thanhXem.SetActive(true);
        ChonXeXemTiepTheo();
    }

    private void ChonXeXemTiepTheo()
    {
        mucTieuXem = null;
        foreach (var xe in FindObjectsByType<XuLyVaChamChoXe>(FindObjectsSortMode.None))
        {
            if (xe != null && xe.gameObject.activeInHierarchy && xe != LayXeCuaMinh())
            {
                mucTieuXem = xe.transform;
                break;
            }
        }
        CapNhatXemText();
    }

    private void ChonXeTheoActor(int actor)
    {
        if (actor < 0) return;
        foreach (var xe in FindObjectsByType<XuLyVaChamChoXe>(FindObjectsSortMode.None))
        {
            if (xe != null && xe.photonView != null && xe.photonView.OwnerActorNr == actor)
            {
                mucTieuXem = xe.transform;
                break;
            }
        }
    }

    private int ActorCuaXe(Transform t)
    {
        if (t == null) return -1;
        PhotonView pv = t.GetComponentInParent<PhotonView>();
        return pv != null ? pv.OwnerActorNr : -1;
    }

    private void CapNhatXemText()
    {
        if (tenXemText == null) return;
        if (mucTieuXem == null)
        {
            tenXemText.text = "SPECTATING (NO ACTIVE RACERS)";
            return;
        }

        PhotonView pv = mucTieuXem.GetComponentInParent<PhotonView>();
        string ten = (pv != null && pv.Owner != null) ? LayTenNguoiChoi(pv.Owner) : "RACER";
        tenXemText.text = $"SPECTATING: {ten.ToUpper()}  [Q / E]";
    }

    // =========================================================================
    // ESC, PAUSE, RESTART & QUIT
    // =========================================================================
    private void XuLyEscNgoaiTranDua()
    {
        if (ketThucGame || winGame) return;

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (ketThucGame || winGame) return;
        isPaused = true;
        if (!cheDoNhieuNguoi) Time.timeScale = 0f;
        if (pausePanelObject != null) pausePanelObject.SetActive(true);
        MoKhoaChuot();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanelObject != null) pausePanelObject.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("menu");
    }
}