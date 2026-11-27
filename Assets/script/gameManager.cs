using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class GameManager : MonoBehaviour
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
    

    [Header("Bắt đầu trận bằng phím (Enter dùng cho chat nên đổi sang phím khác)")]
    public KeyCode phimBatDauTran = KeyCode.F2;

    [Header("Thời gian chờ khi vào scene / bắt đầu đua (theo cách các game đua làm: chờ ổn định -> 3-2-1-GO)")]
    [Tooltip("Sau khi vào scene game, khóa nút START RACE trong ngần này giây để xe / map / mạng kịp ổn định")]
    public float thoiGianOnDinhSauKhiVaoScene = 2f;
    [Tooltip("Chờ tối đa bao lâu cho XE CỦA MÌNH xuất hiện. Quá hạn vẫn cho bắt đầu (tránh kẹt nếu map không có xe)")]
    public float thoiGianToiDaChoXe = 8f;
    [Tooltip("Thời gian đếm ngược trước khi bắt đầu ván nhiều người.")]
    public float demNguocBatDauNhieuNguoi = 3f;
    [Tooltip("Chơi 1 người: đếm ngược sau khi bấm START RACE")]
    public float demNguocBatDauSolo = 1.5f;
    [Tooltip("Sau khi đua bắt đầu, bỏ qua thắng / chạm nước trong ngần này giây (bảo vệ lúc xe vừa xuất hiện)")]
    public float thoiGianBaoVeSauKhiBatDau = 1.5f;

    [Header("Mạng (chỉ nhiều người) - càng cao xe người khác càng mượt, nhưng tốn băng thông hơn")]
    [Tooltip("Tần suất gửi input qua kết nối TCP.")]
    public int tanSoGuiMang = 20;
    [Tooltip("Tần suất cập nhật trạng thái qua kết nối TCP.")]
    public int tanSoDongBo = 10;

    [Header("Hiệu ứng nút (Màu trắng)")]
    public bool hieuUngNut = true;
    public Color mauVienNut = Color.white;

    private const string KEY_NHAC_TAT = "NhacBiTat";

    private static readonly Color MauVang = new Color(1f, 0.85f, 0.15f);
    private static readonly Color MauThua = new Color(1f, 0.35f, 0.35f);
    private static readonly Color MauTongXu = new Color(1f, 0.88f, 0.25f);
    private static readonly Color[] BangMauPhao =
    {
        new Color(1f, 0.85f, 0.15f),
        new Color(0.25f, 0.95f, 0.55f),
        new Color(0.3f, 0.75f, 1f),
        new Color(1f, 0.4f, 0.65f),
        new Color(1f, 0.55f, 0.15f)
    };

    private bool cheDoNhieuNguoi = false;
    private readonly HashSet<int> checkpointRequestsPending = new HashSet<int>();
    private bool eliminationRequestPending;

    // Cho các script khác (HelpSystem...) biết đang chơi 1 người hay nhiều người
    public bool CheDoNhieuNguoi { get { return cheDoNhieuNguoi; } }

    private float thoiGianCoTheQuaLai = 0f;
    private float thoiGianCoTheBiTruNuoc = 0f;
    private float thoiDiemBatDauDua = -1f;
    private bool isPaused = false;
    private bool banThanDaBam = false;
    private Button nutStartRaceCache;
    private TMP_Text chuNutStartRaceCache;
    private bool daTimReadyText = false;

    // Chế độ mà nội dung TutorialPanel đang hiển thị (-1 = chưa vẽ, 0 = solo, 1 = nhiều người)
    private int cheDoTutorialDaVe = -1;

    private bool daCoNguoiThang = false;
    private bool daNhanThuongTranNay = false;
    private int soXuHienThiCuoi = -1;
    private int actorNguoiThang = -1;

    private bool coKetQua = false;
    private bool kqThang = false;
    private int kqXu = 0, kqTong = 0;
    private string kqPhuDe = "";
    private bool daQuetResultPanel = false;
    private bool daKetNoiPause = false;
    private bool daKetNoiEliminate = false;

    private bool nhacBiTat = false;
    private GameObject nutNhac;
    private Image nutNhacIcon;
    private static Sprite spriteLoaBat, spriteLoaTat;

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
    private int hudGiayCuoi = -1;

    private GameObject bannerRoot;
    private Image bannerVach;
    private TMP_Text bannerTieuDe;
    private TMP_Text bannerPhu;
    private Image phuMan;
    private Coroutine coroutinePhuMan;
    private Coroutine coroutineChucMung;
    private readonly List<GameObject> confettiObjs = new List<GameObject>(45);
    private readonly Stack<TMP_Text> poolPopup = new Stack<TMP_Text>();

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
    private Camera camXem;
    private Transform mucTieuXem;
    private float thoiDiemQuetXeTiepTheo = 0f;
    private readonly List<Transform> danhSachXeXem = new List<Transform>(8);
    private int actorXemCache = int.MinValue;
    private string xemTextCache = "";

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

    // Theo dõi lúc bật/tắt chat để khóa phím một nhịp ngắn (tránh lọt phím Enter / phím vừa gõ)
    private bool chatTruoc = false;
    private float thoiDiemChatDoi = -10f;

    // Chờ ổn định / đếm ngược bắt đầu
    private float thoiDiemVaoScene = 0f;
    private bool daOnDinhScene = false;
    private bool dangDemBatDau = false;
    private TMP_Text chuDemNguoc;
    private Coroutine coroutineDemNguoc;

    private readonly Dictionary<GameObject, TMP_Text[]> cacheTextXu = new Dictionary<GameObject, TMP_Text[]>();

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
        if (instance != null && instance != this)
        {
            // Scene cũ có thể chưa kịp bị huỷ sau khi map mới được nạp.
            // Khi đó bản MỚI phải thay thế bản cũ, không được tự xoá mình (xoá cả GameObject sẽ làm map mới
            // mất bảng hướng dẫn / HUD / kết quả). Chỉ bỏ bản thừa khi có 2 GameManager trong CÙNG 1 scene.
            if (instance.gameObject.scene == gameObject.scene)
            {
                Debug.LogWarning("[GameManager] Có 2 GameManager trong cùng scene, bỏ bản thừa trên object: " + name);
                enabled = false;
                Destroy(this);
                return;
            }
        }

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
    // Thu nhỏ bằng localScale nên không bị LayoutGroup ghi đè. Phải chạy TRƯỚC khi ButtonFx gắn vào
    // (ButtonFx lấy scale lúc khởi tạo làm gốc cho hiệu ứng hover) - KetNoiNutPausePanel chạy trong Start trước GanHieuUngChoMoiNut nên đúng thứ tự.
    
    private void OnEnable()
    {
        if (instance == null) instance = this;
        TcpGameplaySync.GameFinished += OnServerGameFinished;
        TcpGameplaySync.CheckpointCollected += OnServerCheckpointCollected;
        TcpGameplaySync.PlayerEliminated += OnServerPlayerEliminated;
    }

    private void OnDisable()
    {
        TcpGameplaySync.GameFinished -= OnServerGameFinished;
        TcpGameplaySync.CheckpointCollected -= OnServerCheckpointCollected;
        TcpGameplaySync.PlayerEliminated -= OnServerPlayerEliminated;
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

    // Tìm panel UI cấp cao nhất trong scene này có tên chứa từ khoá (kể cả đang ẩn).
    // Dùng khi map khác đặt tên panel không đúng như map gốc. Chỉ gọi khi panel còn thiếu.
    private GameObject TimPanelTheoTuKhoa(params string[] tuKhoa)
    {
        GameObject tot = null;
        int doSauTot = int.MaxValue;
        Scene scene = gameObject.scene;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.gameObject.scene != scene) continue;
            if (!(t is RectTransform)) continue;

            string n = t.name.ToLowerInvariant();
            if (n.Contains("text") || n.Contains("button") || n.Contains("btn") || n.Contains("label")) continue;

            bool khop = false;
            for (int i = 0; i < tuKhoa.Length; i++)
            {
                if (n.Contains(tuKhoa[i])) { khop = true; break; }
            }
            if (!khop) continue;

            if (t.GetComponentsInChildren<TMP_Text>(true).Length < 2) continue;

            int doSau = 0;
            for (Transform pr = t.parent; pr != null; pr = pr.parent) doSau++;

            if (doSau < doSauTot)
            {
                doSauTot = doSau;
                tot = t.gameObject;
            }
        }

        return tot;
    }

    // Quét scene MỘT lần cho nhiều tên (thay cho 5 lần quét toàn bộ Transform)
    private Dictionary<string, GameObject> QuetObjectTheoTen(params string[] cacTen)
    {
        var kq = new Dictionary<string, GameObject>(cacTen.Length);
        Scene scene = gameObject.scene;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.gameObject.scene != scene) continue;
            for (int i = 0; i < cacTen.Length; i++)
            {
                if (t.name == cacTen[i] && !kq.ContainsKey(cacTen[i]))
                {
                    kq[cacTen[i]] = t.gameObject;
                    break;
                }
            }
        }
        return kq;
    }

    // Nội dung bảng hướng dẫn trước trận: khác nhau giữa 1 người và nhiều người
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

        if (title != null) title.text = cheDoNhieuNguoi ? "HOW TO PLAY - MULTIPLAYER" : "HOW TO PLAY - SOLO";
        if (content == null) return;

        const string CONTROLS =
            "<b><color=#B8C0C8>CONTROLS</color></b>\n" +
            "W / ↑: Accelerate     S / ↓: Brake or reverse\n" +
            "A / ←: Turn left       D / →: Turn right       SPACE: Jump\n\n";

        const string CHECKPOINT =
            "<b><color=#B8C0C8>CHECKPOINT BONUS</color></b>\n" +
            "Pass a checkpoint: <color=#FFD633><b>+5 SECONDS</b></color>\n" +
            "Off-track: <color=#FF7777><b>-5 SECONDS</b></color> and return to the start.\n\n";

        if (cheDoNhieuNguoi)
        {
            content.text =
                CONTROLS +
                "<b><color=#B8C0C8>OBJECTIVE</color></b>\n" +
                "Be the first to cross the finish line, or be the last racer left. When your timer hits zero you are eliminated and can spectate (Q / E).\n\n" +
                CHECKPOINT +
                "<b><color=#B8C0C8>COINS & REWARDS</color></b>\n" +
                "Win the race: <color=#FFD633><b>+50 COINS</b></color>\n" +
                "Spend coins in the SHOP to unlock 25 custom scooter colors.\n\n" +
                "<b><color=#B8C0C8>MULTIPLAYER</color></b>\n" +
                "Everyone must press START RACE before the race begins. ESC opens the menu but the race keeps running.";
        }
        else
        {
            content.text =
                CONTROLS +
                "<b><color=#B8C0C8>OBJECTIVE</color></b>\n" +
                "Avoid obstacles and reach the finish line before time runs out.\n\n" +
                CHECKPOINT +
                "<b><color=#B8C0C8>COINS & REWARDS</color></b>\n" +
                "Finish the race: <color=#FFD633><b>+10 COINS</b></color>\n" +
                "Spend coins in the SHOP to unlock 25 custom scooter colors.\n\n" +
                "<b><color=#B8C0C8>SOLO</color></b>\n" +
                "Press START RACE to begin. ESC pauses the game.";
        }

        content.fontStyle = FontStyles.Normal;
        content.alignment = TextAlignmentOptions.TopLeft;
        content.textWrappingMode = TextWrappingModes.Normal;
        content.enableAutoSizing = true;
        content.fontSizeMin = 16f;
        content.fontSizeMax = 25f;
        content.paragraphSpacing = 5f;

        cheDoTutorialDaVe = cheDoNhieuNguoi ? 1 : 0;
    }

    // Quét toàn scene: chỉ gọi khi khởi tạo hoặc khi EventSystem bị mất
    private void DamBaoEventSystemDuyNhat()
    {
        EventSystem[] allEs = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem es = null;

        for (int i = 0; i < allEs.Length; i++)
        {
            if (allEs[i] == null) continue;
            if (es == null)
            {
                es = allEs[i];
                es.gameObject.SetActive(true);
                es.enabled = true;
            }
            else
            {
                Destroy(allEs[i].gameObject);
            }
        }

        if (es == null)
        {
            GameObject esObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            esObject.AddComponent<InputSystemUIInputModule>();
#else
            esObject.AddComponent<StandaloneInputModule>();
#endif
            es = esObject.GetComponent<EventSystem>();
        }

        BaseInputModule inputModule = es.GetComponent<BaseInputModule>();
        if (inputModule != null) inputModule.enabled = true;
    }

    private void DamBaoRaycasterChoPanel(GameObject panel)
    {
        if (panel == null) return;
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private static void KichHoatClickChoNut(Button btn)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(true);
        btn.enabled = true;
        btn.interactable = true;

        foreach (Graphic g in btn.GetComponentsInChildren<Graphic>(true))
        {
            if (g != null) g.raycastTarget = true;
        }
    }

    // Chỉ đưa lên cuối danh sách con khi chưa ở cuối (tránh làm bẩn canvas vô ích)
    private static void DatCuoi(Transform t)
    {
        if (t == null || t.parent == null) return;
        if (t.GetSiblingIndex() != t.parent.childCount - 1) t.SetAsLastSibling();
    }

    private static void DatMau(Graphic g, Color c)
    {
        if (g != null && g.color != c) g.color = c;
    }
    

    

    private void KetNoiNutPausePanel()
    {
        if (pausePanelObject == null || daKetNoiPause) return;
        daKetNoiPause = true;

        foreach (Graphic g in pausePanelObject.GetComponentsInChildren<Graphic>(true))
        {
            if (g != null && g.GetComponentInParent<Button>() == null)
                g.raycastTarget = false;
        }

        foreach (Button button in pausePanelObject.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string key = button.gameObject.name.ToLowerInvariant();
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("resume") || key.Contains("continue") || key.Contains("back to game"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ResumeGame);
                KichHoatClickChoNut(button);
            }
            else if (key.Contains("main menu") || key.Contains("back to menu") ||
                     key.Contains("quit") || key.Contains("exit") || key.Contains("menu"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
                KichHoatClickChoNut(button);
            }
        }
    }

    private void KetNoiNutResultPanel()
    {
        if (resultPanelObject == null) return;

        foreach (Graphic g in resultPanelObject.GetComponentsInChildren<Graphic>(true))
        {
            if (g != null && g.GetComponentInParent<Button>() == null)
                g.raycastTarget = false;
        }

        foreach (Button button in resultPanelObject.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string key = button.gameObject.name.ToLowerInvariant();
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("restart") || key.Contains("again") || key.Contains("retry"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(RestartGame);
                KichHoatClickChoNut(button);
            }
            else if (key.Contains("menu") || key.Contains("quit") || key.Contains("exit") || key.Contains("lobby"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
                KichHoatClickChoNut(button);
            }
        }
    }

    // Bảng "bị loại": STAY & WATCH = ẩn bảng để xem tiếp, LEAVE MATCH = rời trận về menu (chỉ kết nối / sửa chữ 1 lần)
    private void KetNoiNutEliminatePanel()
    {
        if (eliminatePanelObject == null || daKetNoiEliminate) return;
        daKetNoiEliminate = true;

        DamBaoRaycasterChoPanel(eliminatePanelObject);

        foreach (Graphic g in eliminatePanelObject.GetComponentsInChildren<Graphic>(true))
        {
            if (g != null && g.GetComponentInParent<Button>() == null)
                g.raycastTarget = false;
        }

        foreach (Button button in eliminatePanelObject.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string key = button.gameObject.name.ToLowerInvariant();
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("stay") || key.Contains("watch") || key.Contains("spectate"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(AnBangLoaiVaXem);
                KichHoatClickChoNut(button);
            }
            else if (key.Contains("leave") || key.Contains("quit") || key.Contains("exit") || key.Contains("menu"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
                KichHoatClickChoNut(button);
            }
        }

        CapNhatChuEliminatePanel();
    }

    private void CapNhatChuEliminatePanel()
    {
        if (eliminatePanelObject == null) return;

        TMP_Text tieuDe = null;
        float yCao = float.MinValue;
        var cacChuKhac = new List<TMP_Text>();

        foreach (TMP_Text t in eliminatePanelObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null || t.GetComponentInParent<Button>() != null) continue;

            float y = t.rectTransform.position.y;
            if (tieuDe == null || y > yCao)
            {
                if (tieuDe != null) cacChuKhac.Add(tieuDe);
                tieuDe = t;
                yCao = y;
            }
            else
            {
                cacChuKhac.Add(t);
            }
        }

        if (tieuDe == null) return;

        tieuDe.gameObject.SetActive(true);
        tieuDe.text = "YOU'VE BEEN ELIMINATED";
        tieuDe.enableAutoSizing = true;
        tieuDe.fontSizeMin = 24f;
        tieuDe.fontSizeMax = Mathf.Max(36f, tieuDe.fontSize);
        tieuDe.textWrappingMode = TextWrappingModes.NoWrap;

        foreach (TMP_Text t in cacChuKhac)
        {
            if (t != null && t.text.Trim().Equals("NEW TEXT", System.StringComparison.OrdinalIgnoreCase))
                t.gameObject.SetActive(false);
        }
    }

    public void AnBangLoaiVaXem()
    {
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
    }

    private void KetNoiNutBatDauTutorial()
    {
        if (tutorialPanelObject == null) return;

        foreach (Graphic g in tutorialPanelObject.GetComponentsInChildren<Graphic>(true))
        {
            if (g != null && g.GetComponentInParent<Button>() == null)
                g.raycastTarget = false;
        }

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
                if (chu.Contains("BẮT ĐẦU") || chu.Contains("START RACE") || chu.Contains("START GAME") || chu.Contains("WAITING"))
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

        nutStartRaceCache = nutBatDau;
        KichHoatClickChoNut(nutBatDau);
        nutBatDau.onClick.RemoveAllListeners();
        nutBatDau.onClick.AddListener(BatDauGame);

        foreach (TMP_Text label in nutBatDau.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label != null)
            {
                chuNutStartRaceCache = label;
                label.text = "START RACE";
            }
        }

        nutBatDau.transform.SetAsLastSibling();
    }

    private void DamBaoTextTrangThaiSanSang()
    {
        if (readyStatusText != null) return;

        if (!daTimReadyText)
        {
            daTimReadyText = true;     // chỉ quét scene 1 lần
            GameObject objText = TimObjectTheoTenChinhXac("ReadyStatusText");
            if (objText != null) readyStatusText = objText.GetComponent<TMP_Text>();
            if (readyStatusText != null) return;
        }

        if (tutorialPanelObject != null)
        {
            GameObject go = new GameObject("ReadyStatusText_Auto", typeof(RectTransform));
            go.transform.SetParent(tutorialPanelObject.transform, false);
            RectTransform r = go.GetComponent<RectTransform>();

            if (nutStartRaceCache != null)
            {
                RectTransform btnRect = nutStartRaceCache.GetComponent<RectTransform>();
                r.anchorMin = btnRect.anchorMin;
                r.anchorMax = btnRect.anchorMax;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = btnRect.anchoredPosition + new Vector2(0f, btnRect.sizeDelta.y * 0.5f + 36f);
                r.sizeDelta = new Vector2(560f, 44f);
            }
            else
            {
                r.anchorMin = r.anchorMax = new Vector2(0.75f, 0.35f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(560f, 44f);
            }

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.88f, 0.25f);
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;
            readyStatusText = tmp;
        }
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
        cheDoNhieuNguoi = !string.IsNullOrWhiteSpace(LauncherArgs.Room);

        // Có người vào/ra phòng làm đổi chế độ khi bảng hướng dẫn đang mở: vẽ lại nội dung cho đúng
        int cheDo = cheDoNhieuNguoi ? 1 : 0;
        if (tutorialPanelObject != null && cheDo != cheDoTutorialDaVe)
            CapNhatNoiDungTutorial();
    }

    private void Start()
    {
        thoiDiemVaoScene = Time.unscaledTime;
        XacDinhCheDo();

        // Quét scene 1 lần duy nhất cho tất cả panel
        var found = QuetObjectTheoTen("TutorialPanel", "ResultPanel", "PausePanel", "EliminatedPanel", "time");
        GameObject g;
        if (found.TryGetValue("TutorialPanel", out g)) tutorialPanelObject = g;
        if (found.TryGetValue("ResultPanel", out g)) resultPanelObject = g;
        if (found.TryGetValue("PausePanel", out g)) pausePanelObject = g;
        if (found.TryGetValue("EliminatedPanel", out g)) eliminatePanelObject = g;
        if (timeGameObject == null && found.TryGetValue("time", out g)) timeGameObject = g;

        // Map khác đặt tên panel khác: tìm thêm theo từ khoá để không sót panel mẫu ("New Text") đang bật sẵn
        if (tutorialPanelObject == null) tutorialPanelObject = TimPanelTheoTuKhoa("tutorial");
        if (resultPanelObject == null) resultPanelObject = TimPanelTheoTuKhoa("result");
        if (pausePanelObject == null) pausePanelObject = TimPanelTheoTuKhoa("pause");
        if (gameOverObject == null) gameOverObject = TimPanelTheoTuKhoa("gameover", "game_over");
        if (winGameObject == null) winGameObject = TimPanelTheoTuKhoa("wingame", "winpanel", "win_game");

        Debug.Log($"[GameManager] scene={gameObject.scene.name} | tutorial={(tutorialPanelObject != null)} result={(resultPanelObject != null)} " +
                  $"pause={(pausePanelObject != null)} gameOver={(gameOverObject != null)} win={(winGameObject != null)} | nhieuNguoi={cheDoNhieuNguoi}");

        CapNhatNoiDungTutorial();
        DamBaoEventSystemDuyNhat();
        DamBaoRaycasterChoPanel(tutorialPanelObject);
        DamBaoRaycasterChoPanel(pausePanelObject);
        DamBaoRaycasterChoPanel(resultPanelObject);
        KetNoiNutBatDauTutorial();
        DatNutDangTai();                 // khóa nút START RACE cho tới khi scene ổn định
        DamBaoTextTrangThaiSanSang();
        KetNoiNutPausePanel();
        KetNoiNutResultPanel();

        danhSachDongXu = GameObject.FindGameObjectsWithTag("CheckPoint");
        System.Array.Sort(danhSachDongXu, CompareCheckpoints);

        if (eliminatePanelObject == null)
            eliminatePanelObject = TimObjectTrongCanvas("eliminate");

        KetNoiNutEliminatePanel();

        if (timeGameObject != null) timeGameObject.SetActive(tutorialPanelObject == null);
        if (gameOverObject != null) gameOverObject.SetActive(false);
        if (winGameObject != null) winGameObject.SetActive(false);
        if (resultPanelObject != null) resultPanelObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        if (pausePanelObject != null) pausePanelObject.SetActive(false);

        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);

        KhoiTaoHienThiXu();
        CapNhatHienThiXu(true);

        hudToiDa = Mathf.Max(1f, thoiGianChoPhepVeDich);
        if (dungHudThoiGianMoi) TaoHudThoiGian();
        if (taoNutNhacTuDong) TaoNutNhac();
        if (hieuUngNut) GanHieuUngChoMoiNut();

        HienThiThoiGianGame();

        if (tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(true);
            tutorialPanelObject.transform.SetAsLastSibling();
            MoKhoaChuot();
            BatNhacNen();

            // Xe và đồng hồ được khóa bởi trạng thái ván; giữ thời gian thực để chat và đồng bộ tiếp tục chạy.
            KiemTraSanSang();
        }
        else
        {
            BatDauDua();
        }
    }

    // =========================================================================
    // BẮT ĐẦU ĐUA & ĐỒNG BỘ SẴN SÀNG
    // Server kiểm tra host và trạng thái phòng trước khi bắt đầu qua TCP.
    // =========================================================================
    public void BatDauGame()
    {
        if (daBatDauDua || dangDemBatDau || !SanSangDeBatDau()) return;
        XacDinhCheDo();
        banThanDaBam = true;
        BatDauDemNguoc(0, cheDoNhieuNguoi ? demNguocBatDauNhieuNguoi : demNguocBatDauSolo);
    }

    private void KiemTraSanSang()
    {
        if (readyStatusText != null && readyStatusText.gameObject.activeSelf)
            readyStatusText.gameObject.SetActive(false);
    }
    private bool SanSangDeBatDau()
    {
        float troi = Time.unscaledTime - thoiDiemVaoScene;
        if (troi < thoiGianOnDinhSauKhiVaoScene) return false;

        // Phải có xe của MÌNH rồi mới được sẵn sàng (tránh vào trận khi xe chưa xuất hiện -> chỉ thấy mình)
        if (LayXeCuaMinh() != null) return true;

        return troi >= thoiGianToiDaChoXe;
    }

    private void DatNutDangTai()
    {
        if (nutStartRaceCache == null) return;
        nutStartRaceCache.interactable = false;
        if (chuNutStartRaceCache != null) chuNutStartRaceCache.text = "LOADING...";
    }

    // Gọi mỗi frame nhưng gần như không tốn gì: chỉ chạy tới lúc scene ổn định 1 lần
    private void CapNhatNutBatDau()
    {
        if (daOnDinhScene || daBatDauDua || dangDemBatDau) return;
        if (!SanSangDeBatDau()) return;

        daOnDinhScene = true;
        if (nutStartRaceCache != null && !banThanDaBam) nutStartRaceCache.interactable = true;
        if (chuNutStartRaceCache != null && !banThanDaBam) chuNutStartRaceCache.text = "START RACE";
    }

    private void BatDauDemNguoc(double ignoredServerTime, float giayLocal)
    {
        if (daBatDauDua || dangDemBatDau) return;
        dangDemBatDau = true;
        if (tutorialPanelObject != null) tutorialPanelObject.SetActive(false);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
        if (coroutineDemNguoc != null) StopCoroutine(coroutineDemNguoc);
        coroutineDemNguoc = StartCoroutine(ChayDemNguocBatDau(0, giayLocal));
    }

    private IEnumerator ChayDemNguocBatDau(double ignoredServerTime, float giayLocal)
    {
        TaoChuDemNguoc();
        float endTime = Time.unscaledTime + Mathf.Max(0f, giayLocal);
        int lastSecond = -1;
        while (Time.unscaledTime < endTime)
        {
            int second = Mathf.CeilToInt(endTime - Time.unscaledTime);
            if (second != lastSecond)
            {
                lastSecond = second;
                HienChuDemNguoc(second.ToString(), new Color(1f, 0.85f, 0.2f));
            }
            yield return null;
        }
        BatDauDua();
        HienChuDemNguoc("GO!", new Color(0.3f, 0.95f, 0.5f));
        yield return new WaitForSecondsRealtime(0.8f);
        AnChuDemNguoc();
        coroutineDemNguoc = null;
    }
    private void TaoChuDemNguoc()
    {
        if (chuDemNguoc != null) return;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        chuDemNguoc = TaoChuCon("ChuDemNguocBatDau", canvas.transform, 140f, Color.white, true);
        RectTransform r = chuDemNguoc.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.55f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(600f, 220f);
        chuDemNguoc.gameObject.SetActive(false);
    }

    private void HienChuDemNguoc(string noiDung, Color mau)
    {
        if (chuDemNguoc == null) return;
        chuDemNguoc.text = noiDung;
        chuDemNguoc.color = mau;
        chuDemNguoc.gameObject.SetActive(true);
        chuDemNguoc.transform.SetAsLastSibling();
    }

    private void AnChuDemNguoc()
    {
        if (chuDemNguoc != null) chuDemNguoc.gameObject.SetActive(false);
    }

    // Vừa xuất phát: bỏ qua thắng / chạm nước một lúc (xe còn đang "ổn định" vật lý)
    private bool DangTrongThoiGianBaoVe()
    {
        return daBatDauDua && Time.time - thoiDiemBatDauDua < thoiGianBaoVeSauKhiBatDau;
    }

    private void BatDauDua()
    {
        if (daBatDauDua) return;
        daBatDauDua = true;
        dangDemBatDau = false;
        thoiDiemBatDauDua = Time.time;

        Time.timeScale = 1f;
        isPaused = false;

        if (tutorialPanelObject != null) tutorialPanelObject.SetActive(false);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
        if (timeGameObject != null) timeGameObject.SetActive(true);

        BatNhacNen();
        HienThiThoiGianGame();
    }

    private void BatNhacNen()
    {
        if (nhacNenSource == null || nhacNenDuaXe == null) return;
        if (nhacNenSource.clip != nhacNenDuaXe) nhacNenSource.clip = nhacNenDuaXe;
        nhacNenSource.mute = nhacBiTat;
        if (!nhacNenSource.isPlaying) nhacNenSource.Play();
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

    private XuLyVaChamChoXe LayXeCuaMinh()
    {
        if (xeCuaMinhCache != null) return xeCuaMinhCache;
        if (Time.unscaledTime < thoiDiemTimXeTiepTheo) return null;
        thoiDiemTimXeTiepTheo = Time.unscaledTime + 0.5f;

        foreach (var xe in FindObjectsByType<XuLyVaChamChoXe>(FindObjectsSortMode.None))
        {
            scooter controller = xe.GetComponentInParent<scooter>();
            if (controller == null || !controller.IsRemoteControlled)
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

        if (!MultiplayerAllInOne.DangGoChat)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow)) ChuyenXeXem(+1);
            else if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow)) ChuyenXeXem(-1);
        }

        // Định kỳ kiểm tra mục tiêu còn hợp lệ (bị loại / rời phòng / bị hủy) - không quét mỗi frame
        if (Time.unscaledTime >= thoiDiemQuetXeTiepTheo)
        {
            thoiDiemQuetXeTiepTheo = Time.unscaledTime + 0.5f;
            LamMoiMucTieuXem();
        }

        if (mucTieuXem == null)
        {
            if (coViTriLucThua) QuanhDiemChet(camXem);
            return;
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
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        HienThongBaoSuKien(lyDo, Color.yellow, Mathf.Min(3f, thoiGianXemNguoiThang));

        ChonXeTheoActor(actorNguoiThang);

        yield return new WaitForSecondsRealtime(thoiGianXemNguoiThang);

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

    // Danh sách text "tổng xu" của mỗi panel được cache, không GetComponentsInChildren lặp lại
    private void CapNhatTextTongXuTrongPanel(GameObject panel, int tongXu)
    {
        if (panel == null) return;

        if (!cacheTextXu.TryGetValue(panel, out TMP_Text[] arr))
        {
            var ds = new List<TMP_Text>(2);
            foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t != null && LaTextTongXu(t.gameObject.name.ToLowerInvariant()))
                    ds.Add(t);
            }
            arr = ds.ToArray();
            cacheTextXu[panel] = arr;
        }

        if (arr.Length == 0) return;
        string s = $"TOTAL COINS: {tongXu}";
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null && arr[i].text != s) arr[i].text = s;
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

    // Popup +/-s dùng pool, không tạo/hủy GameObject mỗi lần
    private void HienPopupThoiGian(string noiDung, Color mau)
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        TMP_Text pop = null;
        while (poolPopup.Count > 0 && pop == null) pop = poolPopup.Pop();

        if (pop == null)
        {
            pop = TaoChuCon("PopupThoiGian", canvas.transform, 34f, mau, true);
            RectTransform nr = pop.rectTransform;
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 1f);
            nr.pivot = new Vector2(0.5f, 1f);
            nr.sizeDelta = new Vector2(260f, 50f);
        }

        pop.rectTransform.anchoredPosition = new Vector2(0f, -78f);
        pop.color = mau;
        pop.text = noiDung;
        pop.gameObject.SetActive(true);
        pop.transform.SetAsLastSibling();

        StartCoroutine(ChayPopupThoiGian(pop, mau));
    }

    private IEnumerator ChayPopupThoiGian(TMP_Text pop, Color mau)
    {
        RectTransform r = pop.rectTransform;
        Vector2 start = r.anchoredPosition;
        const float tong = 0.85f;

        for (float t = 0f; t < tong; t += Time.unscaledDeltaTime)
        {
            if (pop == null) yield break;
            float k = t / tong;
            r.anchoredPosition = start + new Vector2(0f, -24f * k);
            Color c = mau;
            c.a = 1f - k;
            pop.color = c;
            yield return null;
        }

        if (pop != null)
        {
            pop.gameObject.SetActive(false);
            poolPopup.Push(pop);
        }
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
        // Theo dõi bật/tắt chat: sau mỗi lần đổi trạng thái, khóa phím thêm 0.3s để không lọt phím Enter
        bool dangChat = MultiplayerAllInOne.DangGoChat;
        if (dangChat != chatTruoc)
        {
            chatTruoc = dangChat;
            thoiDiemChatDoi = Time.unscaledTime;
        }
        bool khoaBanPhim = dangChat || Time.unscaledTime - thoiDiemChatDoi < 0.3f;

        CapNhatNutBatDau();

        // Bảo vệ TutorialPanel: nếu chưa bắt đầu đua thì tuyệt đối không cho script cũ nào tắt mất TutorialPanel
        // (trừ lúc đang đếm ngược 3-2-1: bảng hướng dẫn đã được ẩn có chủ đích)
        if (!daBatDauDua && !dangDemBatDau && !ketThucGame && !winGame && tutorialPanelObject != null && !tutorialPanelObject.activeSelf)
        {
            tutorialPanelObject.SetActive(true);
            tutorialPanelObject.transform.SetAsLastSibling();
            MoKhoaChuot();
        }

        // Bắt đầu trận bằng phím (mặc định F2). Enter dành cho chat nên không dùng nữa
        if (!khoaBanPhim &&
            !daBatDauDua && tutorialPanelObject != null &&
            tutorialPanelObject.activeInHierarchy &&
            Input.GetKeyDown(phimBatDauTran))
        {
            BatDauGame();
        }

        if (!ketThucGame && !winGame)
        {
            if (resultPanelObject != null && resultPanelObject.activeSelf) resultPanelObject.SetActive(false);
            if (gameOverObject != null && gameOverObject.activeSelf) gameOverObject.SetActive(false);
            if (winGameObject != null && winGameObject.activeSelf) winGameObject.SetActive(false);
        }

        float unscaledNow = Time.unscaledTime;

        if (unscaledNow >= thoiDiemCapNhatXu)
        {
            thoiDiemCapNhatXu = unscaledNow + 0.3f;
            CapNhatHienThiXu();
        }

        CapNhatHudThoiGian();

        // Các việc nhẹ nhưng không cần mỗi frame: gom vào 1 nhịp 0.5 giây
        if (unscaledNow >= thoiDiemKiemTraPanel)
        {
            thoiDiemKiemTraPanel = unscaledNow + 0.5f;

            // Chỉ quét lại toàn scene khi EventSystem thật sự mất (không quét định kỳ)
            if (EventSystem.current == null || !EventSystem.current.isActiveAndEnabled)
                DamBaoEventSystemDuyNhat();

            if (nutNhac != null) DatCuoi(nutNhac.transform);
            if (!daBatDauDua && cheDoNhieuNguoi) KiemTraSanSang();

            if (bannerRoot != null && bannerRoot.activeSelf && CoBangDangMo()) AnBanner();
        }

        if (!khoaBanPhim && Input.GetKeyDown(phimTatNhac))
            BatTatNhac();

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
                    if (xu != null && xu.activeInHierarchy && (xu.transform.position - camPos).sqrMagnitude < 1600f)
                        xu.transform.Rotate(0f, goc, 0f, Space.World);
                }
            }
        }

        if (!khoaBanPhim && InputEscapeUtility.TryConsume())
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

        // Nhiều người: mở menu (ESC) KHÔNG được dừng đồng hồ, vì xe vẫn chạy và người khác vẫn đua
        if (dangDua && (!isPaused || cheDoNhieuNguoi))
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

    // Chỉ chạm vào UI khi giá trị thật sự đổi: không tạo chuỗi / không đổi màu, scale, anchor mỗi frame
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

        hudFillHienTai = Mathf.Abs(hudFillHienTai - tiLe) < 0.001f
            ? tiLe
            : Mathf.Lerp(hudFillHienTai, tiLe, 10f * Time.unscaledDeltaTime);

        Color mau = tiLe > 0.5f ? new Color(0.3f, 0.9f, 0.45f)
                  : tiLe > 0.25f ? new Color(1f, 0.78f, 0.2f)
                  : new Color(1f, 0.3f, 0.3f);

        if (hudFillImg != null)
        {
            if (Mathf.Abs(hudFillRect.anchorMax.x - hudFillHienTai) > 0.0005f)
                hudFillRect.anchorMax = new Vector2(Mathf.Clamp01(hudFillHienTai), 1f);

            DatMau(hudFillImg, mau);

            bool hienFill = hudFillHienTai > 0.01f;
            if (hudFillImg.enabled != hienFill) hudFillImg.enabled = hienFill;
        }

        bool dangNhay = Time.unscaledTime < hudFlashDen;
        bool sapHet = t <= 5f;

        if (hudVien != null)
        {
            Color mv = dangNhay ? new Color(1f, 0.85f, 0.25f) : mau;
            mv.a = dangNhay ? 0.7f : (sapHet ? 0.2f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 0.3f);
            DatMau(hudVien, mv);
        }

        if (hudSo != null)
        {
            int giay = Mathf.CeilToInt(t);
            if (giay != hudGiayCuoi)
            {
                hudGiayCuoi = giay;
                hudSo.text = giay + "<size=55%>s</size>";
            }

            DatMau(hudSo, dangNhay ? new Color(1f, 0.9f, 0.35f) : (sapHet ? mau : Color.white));

            float xung = 1f;
            if (dangNhay) xung = 1f + 0.14f * Mathf.Sin((hudFlashDen - Time.unscaledTime) * 18f);
            else if (sapHet) xung = 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));

            if (dangNhay || sapHet || hudSo.transform.localScale.x != 1f)
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
    // (mỗi người chỉ xử lý checkpoint / nước của chính mình - không đồng bộ qua mạng)
    // =========================================================================
    public void QuaCheckPoint()
    {
        if (!daBatDauDua || ketThucGame || winGame || daBiLoai) return;
        if (Time.time < thoiGianCoTheQuaLai) return;

        thoiGianCoTheQuaLai = Time.time + 0.005f;
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
        if (other == null || !other.gameObject.activeSelf || !daBatDauDua || ketThucGame || winGame || daBiLoai)
            return;

        if (cheDoNhieuNguoi)
            return;

        other.gameObject.SetActive(false);
        QuaCheckPoint();
    }

    private static int CompareCheckpoints(GameObject a, GameObject b)
    {
        int byIndex = GetCheckpointIndex(a.name).CompareTo(GetCheckpointIndex(b.name));
        if (byIndex != 0) return byIndex;
        int byName = string.CompareOrdinal(a.name, b.name);
        if (byName != 0) return byName;
        int byX = a.transform.position.x.CompareTo(b.transform.position.x);
        return byX != 0 ? byX : a.transform.position.z.CompareTo(b.transform.position.z);
    }

    private static int GetCheckpointIndex(string name)
    {
        int open = name.LastIndexOf('(');
        if (open >= 0 && name.EndsWith(")", StringComparison.Ordinal) &&
            int.TryParse(name.Substring(open + 1, name.Length - open - 2), out int index))
            return index;
        return 0;
    }

    private void OnServerCheckpointCollected(int index)
    {
        if (!cheDoNhieuNguoi || danhSachDongXu == null || index < 1 || index > danhSachDongXu.Length)
            return;

        GameObject checkpoint = danhSachDongXu[index - 1];
        if (checkpoint == null || !checkpoint.activeSelf)
            return;
        checkpoint.SetActive(false);
        QuaCheckPoint();
    }

    public void ChamNuoc()
    {
        if (!daBatDauDua || ketThucGame || winGame || daBiLoai || DangTrongThoiGianBaoVe()) return;
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

    // Giữ nguyên chữ ký cũ (script khác / SendMessage vẫn gọi được)
    public void VeDichThanhCong()
    {
        if (!daBatDauDua || DangTrongThoiGianBaoVe())
        {
            Debug.LogWarning("[GameManager] Finish event ignored before the race is ready.");
            return;
        }

        if (!cheDoNhieuNguoi)
        {
            XuLyThang(false);
            return;
        }
        Debug.Log("[GameManager] Multiplayer finish is validated by the server.");
    }

    private void OnServerGameFinished(string winnerUsername)
    {
        if (!cheDoNhieuNguoi || ketThucGame || winGame || daCoNguoiThang)
            return;

        if (string.Equals(winnerUsername, LauncherArgs.User, StringComparison.OrdinalIgnoreCase))
        {
            XuLyThang(false);
            return;
        }

        daCoNguoiThang = true;
        string name = string.IsNullOrWhiteSpace(winnerUsername) ? "ANOTHER PLAYER" : winnerUsername.ToUpperInvariant();
        HienKetQuaThua(name + " WON THE RACE!", true);
    }

    private void OnServerPlayerEliminated(string username)
    {
        eliminationRequestPending = false;
        if (cheDoNhieuNguoi && string.Equals(username, LauncherArgs.User, StringComparison.OrdinalIgnoreCase))
            XuLyBanThanBiLoaiNoiBo();
    }

    private void XuLyThang(bool thangVoiLyDoConLaiCuoiCung)
    {
        if (!daBatDauDua || ketThucGame || winGame || daBiLoai || daCoNguoiThang) return;

        daCoNguoiThang = true;
        winGame = true;
        ketThucGame = true;

        int xuNhan = cheDoNhieuNguoi ? 0 : thuongChoiSolo;
        if (!cheDoNhieuNguoi && !daNhanThuongTranNay && xuNhan > 0)
        {
            daNhanThuongTranNay = true;
            PlayerInventory.AddCoins(xuNhan);
            CapNhatHienThiXu(true);
        }

        if (!nhacBiTat && hieuUngSource != null && amThanhWin != null)
            hieuUngSource.PlayOneShot(amThanhWin);

        MatchHistorySystem.LuuKetQuaTran(true, xuNhan);

        string loiChucPhuDe;
        if (!cheDoNhieuNguoi) loiChucPhuDe = "YOU CROSSED THE FINISH LINE!";
        else if (thangVoiLyDoConLaiCuoiCung) loiChucPhuDe = "YOU ARE THE LAST RACER STANDING!";
        else loiChucPhuDe = "YOU REACHED THE FINISH LINE FIRST!";

        HienBangKetQuaCuoi(true, xuNhan, loiChucPhuDe);
    }

    private void XuLyBanThanBiLoai()
    {
        if (!cheDoNhieuNguoi)
        {
            XuLyBanThanBiLoaiNoiBo();
            return;
        }
        if (eliminationRequestPending || daBiLoai || ketThucGame || winGame)
            return;

        eliminationRequestPending = true;
        SubmitEliminationAsync();
    }

    private async void SubmitEliminationAsync()
    {
        try
        {
            TcpGameplaySync sync = FindFirstObjectByType<TcpGameplaySync>();
            if (sync == null)
                throw new InvalidOperationException("TCP gameplay synchronizer is unavailable.");
            Resp response = await sync.SendGameEventAsync(new Req
            {
                command = "GAME_EVENT",
                gameEventType = "ELIMINATE"
            });
            if (response == null || !response.success)
            {
                eliminationRequestPending = false;
                Debug.LogWarning("[GameManager] Server rejected the elimination event.");
            }
        }
        catch (Exception ex)
        {
            eliminationRequestPending = false;
            Debug.LogWarning("[GameManager] Elimination event failed: " + ex.GetType().Name);
        }
    }

    private void XuLyBanThanBiLoaiNoiBo()
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

        if (cheDoNhieuNguoi)
        {
            if (eliminatePanelObject != null)
            {
                KetNoiNutEliminatePanel();     // chỉ thực hiện lần đầu
                eliminatePanelObject.SetActive(true);
                DatCuoi(eliminatePanelObject.transform);
            }

            BatDauCheDoXemTran();
            return;
        }

        HienKetQuaThua("TIME'S UP!", false);
    }
    private void HienKetQuaThua(string lyDo, bool tatXemTran)
    {
        if (coKetQua) return;      // chỉ hiện kết quả 1 lần
        if (!daBatDauDua)
        {
            Debug.LogWarning("[GameManager] Bỏ qua bảng kết quả vì đua chưa bắt đầu: " + lyDo);
            return;
        }

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
        if (coKetQua) return;      // chống hiện / cộng thưởng trùng
        coKetQua = true;

        // Ghi lại LÝ DO + thời điểm hiện bảng kết thúc để dò lỗi (xem trong Console)
        Debug.LogWarning($"[GameManager] HIỆN BẢNG KẾT THÚC thang={thang} lyDo='{phuDe}' " +
                         $"saiVaoScene={Time.unscaledTime - thoiDiemVaoScene:F2}s " +
                         $"saiBatDau={(daBatDauDua ? Time.time - thoiDiemBatDauDua : -1f):F2}s\n{StackTraceUtility.ExtractStackTrace()}");
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
            TaoThemNutVePhongCho();
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

    // Gán các dòng chữ của ResultPanel (chỉ 1 lần): ưu tiên theo tên / chữ mẫu,
    // phần còn lại ("New Text") gán theo thứ tự từ trên xuống
    private void QuetResultPanelMotLan()
    {
        if (daQuetResultPanel || resultPanelObject == null) return;
        daQuetResultPanel = true;

        var conLai = new List<TMP_Text>();

        foreach (TMP_Text t in resultPanelObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null || t.GetComponentInParent<Button>() != null) continue;
            if (t == resultTitleText || t == resultSubtitleText || t == resultRewardText || t == resultTotalCoinsText) continue;

            string n = t.gameObject.name.ToLowerInvariant();
            string st = (t.text ?? "").ToLowerInvariant();

            if (resultSubtitleText == null && n.Contains("sub")) resultSubtitleText = t;
            else if (resultRewardText == null && n.Contains("reward")) resultRewardText = t;
            else if (resultTotalCoinsText == null &&
                     (n.Contains("total") || n.Contains("balance") || n.Contains("tong") || st.Contains("tổng") || st.Contains("total")))
                resultTotalCoinsText = t;
            else if (resultTitleText == null &&
                     (n.Contains("title") || st.Contains("tiêu đề") || st.Contains("tieu de")))
                resultTitleText = t;
            else conLai.Add(t);
        }

        conLai.Sort((x, y) => y.rectTransform.position.y.CompareTo(x.rectTransform.position.y));

        foreach (TMP_Text t in conLai)
        {
            if (resultTitleText == null) resultTitleText = t;
            else if (resultSubtitleText == null) resultSubtitleText = t;
            else if (resultRewardText == null) resultRewardText = t;
            else if (resultTotalCoinsText == null) resultTotalCoinsText = t;
            else if (anChuThuaTrongResult &&
                     (t.text.Trim().Equals("NEW TEXT", System.StringComparison.OrdinalIgnoreCase) ||
                      t.gameObject.name.ToLowerInvariant().Contains("detail")))
                t.gameObject.SetActive(false);
        }

        if (resultTitleText != null) resultTitleText.gameObject.SetActive(true);
        if (resultSubtitleText != null) resultSubtitleText.gameObject.SetActive(true);
        if (resultRewardText != null) resultRewardText.gameObject.SetActive(true);
        if (resultTotalCoinsText != null) resultTotalCoinsText.gameObject.SetActive(true);

        if (tuSapXepBoCucResult)
        {
            CanChinhDongTextResult(resultTitleText, 145f, 900f, 80f);
            CanChinhDongTextResult(resultSubtitleText, 75f, 900f, 55f);
            CanChinhDongTextResult(resultRewardText, 10f, 700f, 50f);
            CanChinhDongTextResult(resultTotalCoinsText, -48f, 700f, 46f);
        }

        if (resultTitleText != null)
        {
            resultTitleText.enableAutoSizing = true;
            resultTitleText.fontSizeMin = 28f;
            resultTitleText.fontSizeMax = 62f;
        }
    }

    private void CapNhatNoiDungResultPanel()
    {
        if (!coKetQua || resultPanelObject == null) return;

        QuetResultPanelMotLan();

        if (resultTitleText != null)
        {
            if (kqThang)
                resultTitleText.text = cheDoNhieuNguoi ? "CONGRATULATIONS!" : "RACE COMPLETED!";
            else
                resultTitleText.text = "GAME OVER";

            resultTitleText.color = kqThang ? MauVang : MauThua;
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
            resultTotalCoinsText.color = MauTongXu;
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

        RectTransform panelRect = resultPanelObject.GetComponent<RectTransform>();
        float rong = panelRect != null && panelRect.rect.width > 100f ? panelRect.rect.width : 1200f;
        float cao = panelRect != null && panelRect.rect.height > 100f ? panelRect.rect.height : 700f;

        const int SO_MANH = 45;
        RectTransform[] manhPhao = new RectTransform[SO_MANH];
        Vector2[] vanToc = new Vector2[SO_MANH];
        float[] tocDoXoay = new float[SO_MANH];

        for (int i = 0; i < SO_MANH; i++)
        {
            GameObject go = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(resultPanelObject.transform, false);
            confettiObjs.Add(go);

            Image img = go.GetComponent<Image>();
            img.color = BangMauPhao[i % BangMauPhao.Length];
            img.raycastTarget = false;

            RectTransform r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(UnityEngine.Random.Range(-rong * 0.45f, rong * 0.45f), UnityEngine.Random.Range(0f, cao * 0.45f));
            r.sizeDelta = new Vector2(UnityEngine.Random.Range(10f, 18f), UnityEngine.Random.Range(6f, 12f));

            manhPhao[i] = r;
            vanToc[i] = new Vector2(UnityEngine.Random.Range(-70f, 70f), UnityEngine.Random.Range(-180f, -90f));
            tocDoXoay[i] = UnityEngine.Random.Range(-360f, 360f);
        }

        float t = 0f;
        while (resultPanelObject != null && resultPanelObject.activeInHierarchy)
        {
            float dt = Time.unscaledDeltaTime;     // chạy được cả khi timeScale = 0
            t += dt;

            if (resultTitleText != null)
            {
                resultTitleText.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 5f));
                resultTitleText.color = Color.Lerp(MauVang, Color.white, 0.35f + 0.35f * Mathf.Sin(t * 4f));
            }

            for (int i = 0; i < SO_MANH; i++)
            {
                RectTransform r = manhPhao[i];
                if (r == null) continue;

                Vector2 pos = r.anchoredPosition + vanToc[i] * dt;
                pos.x += Mathf.Sin(t * 3f + i) * 35f * dt;
                if (pos.y < -cao * 0.5f)
                {
                    pos.y = cao * 0.5f;
                    pos.x = UnityEngine.Random.Range(-rong * 0.45f, rong * 0.45f);
                }

                r.anchoredPosition = pos;
                r.Rotate(0f, 0f, tocDoXoay[i] * dt);
            }

            yield return null;
        }

        // Dọn confetti khi panel đóng
        for (int i = 0; i < confettiObjs.Count; i++)
            if (confettiObjs[i] != null) Destroy(confettiObjs[i]);
        confettiObjs.Clear();
        coroutineChucMung = null;
    }

    // =========================================================================
    // SPECTATOR (XEM TRẬN) - chỉ dùng trong multiplayer
    // Danh sách xe được cache, làm mới theo nhịp thay vì FindObjectsByType liên tục.
    // =========================================================================
    private void BatDauCheDoXemTran()
    {
        if (!cheDoNhieuNguoi) return;      // Solo không tạo Spectator

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
                r.sizeDelta = new Vector2(680f, 54f);
                thanhXem = nen.gameObject;

                tenXemText = TaoChuCon("TenXem", thanhXem.transform, 22f, Color.white, true);
                Stretch(tenXemText.rectTransform);
            }
        }

        if (thanhXem != null) thanhXem.SetActive(true);
        thoiDiemQuetXeTiepTheo = Time.unscaledTime + 0.5f;
        actorXemCache = int.MinValue;
        ChonXeXemTiepTheo();
    }

    private void LamMoiDanhSachXe()
    {
        danhSachXeXem.Clear();
        XuLyVaChamChoXe xeMinh = LayXeCuaMinh();

        var tatCa = FindObjectsByType<XuLyVaChamChoXe>(FindObjectsSortMode.None);
        for (int i = 0; i < tatCa.Length; i++)
        {
            XuLyVaChamChoXe xe = tatCa[i];
            if (xe == null || !xe.gameObject.activeInHierarchy || xe == xeMinh) continue;

            scooter controller = xe.GetComponentInParent<scooter>();
            if (controller == null || !controller.IsRemoteControlled) continue;

            danhSachXeXem.Add(xe.transform);
        }
    }

    // Giữ mục tiêu nếu còn hợp lệ; mất mục tiêu (bị loại / rời phòng) thì tự tìm người còn đua
    private void LamMoiMucTieuXem()
    {
        LamMoiDanhSachXe();

        if (mucTieuXem == null || !danhSachXeXem.Contains(mucTieuXem))
            mucTieuXem = danhSachXeXem.Count > 0 ? danhSachXeXem[0] : null;

        CapNhatXemText();
    }

    // Q = -1, E = +1: chuyển vòng qua các người chơi còn hoạt động
    private void ChuyenXeXem(int huong)
    {
        LamMoiDanhSachXe();
        if (danhSachXeXem.Count == 0)
        {
            mucTieuXem = null;
            CapNhatXemText();
            return;
        }

        int idx = danhSachXeXem.IndexOf(mucTieuXem);
        idx = idx < 0 ? 0 : (idx + huong + danhSachXeXem.Count) % danhSachXeXem.Count;
        mucTieuXem = danhSachXeXem[idx];
        CapNhatXemText();
    }

    private void ChonXeXemTiepTheo()
    {
        LamMoiMucTieuXem();
    }

    private void ChonXeTheoActor(int actor)
    {
        if (actor < 0) return;
        LamMoiDanhSachXe();
        for (int i = 0; i < danhSachXeXem.Count; i++)
        {
            if (ActorCuaXe(danhSachXeXem[i]) == actor)
            {
                mucTieuXem = danhSachXeXem[i];
                break;
            }
        }
        CapNhatXemText();
    }

    private int ActorCuaXe(Transform t)
    {
        if (t == null) return -1;
        return t.GetInstanceID();
    }

    // Chỉ dựng lại chuỗi khi mục tiêu đổi
    private void CapNhatXemText()
    {
        if (tenXemText == null) return;

        int actor = mucTieuXem != null ? ActorCuaXe(mucTieuXem) : -1;
        if (actor == actorXemCache && !string.IsNullOrEmpty(xemTextCache)) return;
        actorXemCache = actor;

        if (mucTieuXem == null)
        {
            xemTextCache = "SPECTATING (NO ACTIVE RACERS)   |   [ESC] MENU";
        }
        else
        {
            string ten = mucTieuXem.name.Replace("XE_REMOTE_", "RACER #");
            xemTextCache = $"SPECTATING: {ten.ToUpper()}  [Q / E]   |   [ESC] MENU";
        }
        tenXemText.text = xemTextCache;
    }

    // =========================================================================
    // ESC, PAUSE, RESTART & QUIT
    // =========================================================================
    // ESC dùng được ở mọi trạng thái: trước trận, đang đua, bị loại / xem trận, màn kết quả
    // Thứ tự ưu tiên: thoát focus chat -> đóng bảng trợ giúp/xếp hạng -> đóng menu tạm dừng -> ẩn bảng bị loại -> mở menu
    private void XuLyEscNgoaiTranDua()
    {
        if (MultiplayerAllInOne.DongChatNeuDangGo()) return;
        if (HelpSystem.DongNeuDangMo() || MenuLeaderboard.DongNeuDangMo()) return;

        if (isPaused || DangMo(pausePanelObject))
        {
            ResumeGame();
            return;
        }

        if (DangMo(eliminatePanelObject))
        {
            AnBangLoaiVaXem();
            return;
        }

        PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        // Solo: dừng game. Multiplayer: KHÔNG dừng để không ảnh hưởng người khác
        if (!cheDoNhieuNguoi) Time.timeScale = 0f;

        if (pausePanelObject != null)
        {
            pausePanelObject.SetActive(true);
            pausePanelObject.transform.SetAsLastSibling();
            KetNoiNutPausePanel();     // chỉ kết nối nút 1 lần
        }
        MoKhoaChuot();
    }

    public void ResumeGame()
    {
        isPaused = false;

        // Do not pause world time while starting the countdown; network requests still need to run.
        Time.timeScale = 1f;

        if (pausePanelObject != null) pausePanelObject.SetActive(false);
    }

    public void RestartGame()
    {
        VePhongChung();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        // Báo cho Lobby biết vừa thoát game: bắt người chơi chờ vài giây rồi mới được tạo / vào phòng mới
        VaoGameCooldown.BatDau();

        LeaveTcpRoom();

        SceneManager.LoadScene("menu");
    }
    [Header("Về phòng chờ (Solo & Nhiều người)")]
    [Tooltip("Tên scene phòng chờ / sảnh chọn xe & map")]
    public string tenScenePhongCho = "SelectScene";
    [Tooltip("Tên scene lobby cho chế độ nhiều người.")]
    public string tenSceneLobbyNhieuNguoi = "LobbyScene";
    public string chuNutVeLobbyNhieuNguoi = "BACK TO LOBBY";

    private bool dangVePhongChung = false;

    /// <summary>
    /// Hàm chung: chạy được cả khi chơi 1 mình lẫn khi chơi nhiều người cùng phòng.
    /// Trả người chơi về lobby và rời phòng TCP hiện tại.
    /// </summary>
    public void VePhongChung()
    {
        if (dangVePhongChung) return;
        dangVePhongChung = true;
        LobbyManager.DanhDauCoSanPhongTuVanTruoc();

        Time.timeScale = 1f;
        isPaused = false;
        daBatDauDua = false;
        dangDemBatDau = false;
        ketThucGame = false;
        winGame = false;
        coKetQua = false;
        daCoNguoiThang = false;
        daNhanThuongTranNay = false;
        banThanDaBam = false;
        dangXemTran = false;
        dangQuanhXe = false;
        coViTriLucThua = false;
        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            LeaveTcpRoom();

            string sceneLobby = tenSceneLobbyNhieuNguoi;
            if (string.IsNullOrEmpty(sceneLobby) || !Application.CanStreamedLevelBeLoaded(sceneLobby))
            {
                Debug.LogError($"[GameManager] Không tìm thấy scene '{sceneLobby}' trong Build Settings, tạm dùng '{tenScenePhongCho}'.");
                sceneLobby = tenScenePhongCho;
            }

            if (!string.IsNullOrEmpty(sceneLobby))
            {
                SceneManager.LoadScene(sceneLobby);
                return;
            }
        }
        else if (!string.IsNullOrEmpty(tenScenePhongCho))
        {
            SceneManager.LoadScene(tenScenePhongCho);
            return;
        }

        // Dự phòng nếu để trống tenScenePhongCho: mở lại panel chờ ngay trong scene hiện tại
        if (resultPanelObject != null) resultPanelObject.SetActive(false);
        if (winGameObject != null) winGameObject.SetActive(false);
        if (gameOverObject != null) gameOverObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        if (pausePanelObject != null) pausePanelObject.SetActive(false);
        if (thanhXem != null) thanhXem.SetActive(false);

        if (tutorialPanelObject != null)
        {
            tutorialPanelObject.SetActive(true);
            tutorialPanelObject.transform.SetAsLastSibling();
            MoKhoaChuot();
            XacDinhCheDo();
            KiemTraSanSang();
        }

        dangVePhongChung = false;
    }
    [Header("Tự thêm nút về phòng chờ (SelectScene)")]
    [Tooltip("Bật để tự tạo thêm 1 nút chuyển về phòng chờ trên bảng kết quả")]
    public bool tuThemNutVePhongCho = true;

    [Tooltip("Chữ hiển thị trên nút mới tạo")]
    public string chuNutVePhongCho = "LOBBY / PLAY AGAIN";

    [Tooltip("Độ lệch (X, Y) so với nút hiện có trên bảng kết quả")]
    public Vector2 doLechNutMoi = new Vector2(0f, -70f);

    private bool daTaoNutVePhongCho = false;

    /// <summary>
    /// Gọi hàm này ngay khi mở bảng kết quả (GAME OVER / Result) để tự sinh thêm 1 nút về phòng chờ.
    /// </summary>
    public void TaoThemNutVePhongCho()
    {
        if (!tuThemNutVePhongCho || daTaoNutVePhongCho) return;

        GameObject panel = resultPanelObject != null ? resultPanelObject :
            (gameOverObject != null ? gameOverObject : winGameObject);
        if (panel == null) return;

        Button nutMau = panel.GetComponentInChildren<Button>(true);
        if (nutMau == null) return;

        GameObject nutMoiObj = Instantiate(nutMau.gameObject, nutMau.transform.parent);
        nutMoiObj.name = "Btn_VePhongCho_SelectScene";
        nutMoiObj.SetActive(true);

        RectTransform rtMau = nutMau.GetComponent<RectTransform>();
        RectTransform rtMoi = nutMoiObj.GetComponent<RectTransform>();
        if (rtMau != null && rtMoi != null)
        {
            rtMoi.anchorMin = rtMau.anchorMin;
            rtMoi.anchorMax = rtMau.anchorMax;
            rtMoi.pivot = rtMau.pivot;
            rtMoi.localScale = rtMau.localScale;

            Canvas.ForceUpdateCanvases();
            float wGoc = rtMau.rect.width > 0f ? rtMau.rect.width * rtMau.localScale.x : rtMau.sizeDelta.x;
            float gap = 16f;
            float halfStep = (wGoc + gap) * 0.5f;

            // Dịch nút gốc sang trái nửa bước, nút mới đặt ngay bên phải cùng hàng ngang
            rtMau.anchoredPosition = new Vector2(rtMau.anchoredPosition.x - halfStep, rtMau.anchoredPosition.y);
            rtMoi.sizeDelta = rtMau.sizeDelta;
            rtMoi.anchoredPosition = new Vector2(rtMau.anchoredPosition.x + wGoc + gap, rtMau.anchoredPosition.y) + doLechNutMoi;
        }

        string chu = cheDoNhieuNguoi ? chuNutVeLobbyNhieuNguoi : chuNutVePhongCho;
        foreach (TMP_Text t in nutMoiObj.GetComponentsInChildren<TMP_Text>(true))
            t.text = chu;
        Button btnMoi = nutMoiObj.GetComponent<Button>();
        if (btnMoi != null)
        {
            btnMoi.onClick.RemoveAllListeners();
            btnMoi.onClick.AddListener(VePhongChung);
            daTaoNutVePhongCho = true;
        }
    }

    /// <summary>
    /// Nút thoát hẳn ra menu (ai không muốn chơi tiếp thì bấm nút này).
    /// </summary>
    public void RoiPhongVaVeMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        VaoGameCooldown.BatDau();

        LeaveTcpRoom();

        SceneManager.LoadScene("menu");
    }

    private static void LeaveTcpRoom()
    {
        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room) && ServerClient.Ready)
            ServerClient.Fire(new Req { command = "ROOM_LEAVE" });
        LauncherArgs.Room = string.Empty;
        LauncherArgs.RoomMap = string.Empty;
        LauncherArgs.SpawnIndex = 0;
        LauncherArgs.IsRoomHost = false;
    }
}
