using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuLobbyMusic : MonoBehaviour
{
    public AudioClip nhacMenuLobby;
    [Range(0f, 1f)] public float amLuong = 0.45f;

    [Header("Nhạc sảnh chỉ phát ở các Scene này")]
    public string[] danhSachSceneSanh = { "menu", "ShopScene", "LobbyScene", "SelectScene" };

    [Header("Nút mute (tự tạo, không cần làm UI)")]
    public bool tuTaoNutMute = true;
    public bool hienNutTrongTran = false;                          // bật nếu muốn nút hiện cả khi đang đua
    public Vector2 viTriNut = new Vector2(-30f, -30f);             // lệch so với góc trên phải
    public float kichThuocNut = 70f;
    [Tooltip("Để trống thì tự vẽ icon loa bằng code")]
    public Sprite iconLoaBat;
    public Sprite iconLoaTat;

    private const string KEY_NHAC = "NhacBat"; // dùng chung với MenuController

    private static MenuLobbyMusic instance;
    private static Sprite spriteBatCache;
    private static Sprite spriteTatCache;

    private AudioSource audioSource;
    private bool daTamDung = false;
    private bool nhacDangBat = true;

    private GameObject canvasNut;
    private Image iconImage;

    // =========================================================================
    // KHỞI TẠO
    // =========================================================================
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        nhacDangBat = PlayerPrefs.GetInt(KEY_NHAC, 1) == 1;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = nhacMenuLobby;
        audioSource.loop = true;
        audioSource.volume = amLuong;
        audioSource.playOnAwake = false;

        if (tuTaoNutMute) TaoNutMute();
        ApDungNhac();

        if (nhacMenuLobby != null) audioSource.Play();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += KhiChuyenScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= KhiChuyenScene;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // =========================================================================
    // BẬT / TẮT NHẠC (mute toàn bộ âm thanh)
    // =========================================================================
    public static void BatTat()
    {
        if (instance != null) instance.BatTatNhac();
    }

    public void BatTatNhac()
    {
        nhacDangBat = !nhacDangBat;
        PlayerPrefs.SetInt(KEY_NHAC, nhacDangBat ? 1 : 0);
        PlayerPrefs.Save();
        ApDungNhac();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ApDungNhac()
    {
        AudioListener.volume = nhacDangBat ? 1f : 0f;

        if (iconImage != null)
            iconImage.sprite = nhacDangBat ? LayIconBat() : LayIconTat();
    }

    // =========================================================================
    // CHUYỂN SCENE
    // =========================================================================
    private bool LaSceneSanh(string tenScene)
    {
        return System.Array.IndexOf(danhSachSceneSanh, tenScene) >= 0;
    }

    private void KhiChuyenScene(Scene scene, LoadSceneMode mode)
    {
        bool laSanh = LaSceneSanh(scene.name);

        // Nút mute chỉ hiện ở sảnh (hoặc mọi nơi nếu bật hienNutTrongTran)
        if (canvasNut != null)
        {
            bool hien = laSanh || hienNutTrongTran;
            canvasNut.SetActive(hien);
            if (hien) DamBaoEventSystem();
        }

        if (audioSource == null || nhacMenuLobby == null) return;

        if (laSanh)
        {
            // Về sảnh: tiếp tục nhạc đang dừng, hoặc phát mới nếu chưa phát
            if (daTamDung)
            {
                audioSource.UnPause();
                daTamDung = false;
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // Vào map đua: tạm dừng nhạc sảnh (GameManager tự bật nhạc đua)
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                daTamDung = true;
            }
        }
    }

    // Scene nào chưa có EventSystem thì nút không bấm được, nên tự thêm
    private void DamBaoEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // =========================================================================
    // TỰ TẠO NÚT MUTE
    // =========================================================================
    private void TaoNutMute()
    {
        canvasNut = new GameObject("NutMuteCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasNut.transform.SetParent(transform, false);

        Canvas canvas = canvasNut.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // luôn nằm trên các UI khác

        CanvasScaler scaler = canvasNut.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject nut = new GameObject("NutMute", typeof(RectTransform), typeof(Image), typeof(Button));
        nut.transform.SetParent(canvasNut.transform, false);

        RectTransform rect = nut.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = viTriNut;
        rect.sizeDelta = new Vector2(kichThuocNut, kichThuocNut);

        Image nen = nut.GetComponent<Image>();
        nen.color = new Color(0f, 0f, 0f, 0.55f);

        Button btn = nut.GetComponent<Button>();
        btn.targetGraphic = nen;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None; // tránh Space/Enter bấm nhầm nút
        btn.navigation = nav;
        btn.onClick.AddListener(BatTatNhac);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(nut.transform, false);
        RectTransform ir = iconObj.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(10f, 10f);
        ir.offsetMax = new Vector2(-10f, -10f);

        iconImage = iconObj.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
    }

    // =========================================================================
    // VẼ ICON LOA BẰNG CODE (không cần file ảnh)
    // =========================================================================
    private Sprite LayIconBat()
    {
        if (iconLoaBat != null) return iconLoaBat;
        if (spriteBatCache == null) spriteBatCache = VeIconLoa(true);
        return spriteBatCache;
    }

    private Sprite LayIconTat()
    {
        if (iconLoaTat != null) return iconLoaTat;
        if (spriteTatCache == null) spriteTatCache = VeIconLoa(false);
        return spriteTatCache;
    }

    private static Sprite VeIconLoa(bool bat)
    {
        const int N = 128;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                // 3x3 điểm mẫu cho mép mịn
                int trung = 0;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float u = (x + (sx + 0.5f) / 3f) / N;
                        float v = (y + (sy + 0.5f) / 3f) / N;
                        if (TrongHinhLoa(u, v, bat)) trung++;
                    }
                px[y * N + x] = new Color32(255, 255, 255, (byte)(trung * 255 / 9));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool TrongHinhLoa(float u, float v, bool bat)
    {
        // Thân loa
        if (u >= 0.17f && u <= 0.33f && v >= 0.39f && v <= 0.61f) return true;

        // Chóp loa (loe dần sang phải)
        if (u > 0.33f && u <= 0.52f)
        {
            float nua = 0.11f + (u - 0.33f) / 0.19f * 0.17f;
            if (Mathf.Abs(v - 0.5f) <= nua) return true;
        }

        if (bat)
        {
            // 3 sóng âm
            float dx = u - 0.56f;
            float dy = v - 0.5f;
            if (dx > 0f)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float goc = Mathf.Atan2(Mathf.Abs(dy), dx) * Mathf.Rad2Deg;
                if (goc <= 48f)
                {
                    if (Mathf.Abs(d - 0.13f) <= 0.0225f) return true;
                    if (Mathf.Abs(d - 0.23f) <= 0.0225f) return true;
                    if (Mathf.Abs(d - 0.33f) <= 0.0225f) return true;
                }
            }
        }
        else
        {
            // Dấu X
            if (KhoangCachDenDoan(u, v, 0.63f, 0.39f, 0.86f, 0.61f) <= 0.027f) return true;
            if (KhoangCachDenDoan(u, v, 0.86f, 0.39f, 0.63f, 0.61f) <= 0.027f) return true;
        }

        return false;
    }

    private static float KhoangCachDenDoan(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax, aby = by - ay;
        float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
        float cx = ax + abx * t, cy = ay + aby * t;
        return Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }
}