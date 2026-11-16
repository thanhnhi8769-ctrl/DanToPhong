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
    [Tooltip("Bật: khi mọi người khác bị loại (hết giờ) thì người còn lại thắng ngay. Tắt: người còn lại phải tự về đích. " +
             "Người khác THOÁT PHÒNG thì không bao giờ tự động thắng.")]
    public bool tuDongThangKhiConMotNguoi = true;

    [Header("Hiệu ứng đồng xu CheckPoint")]
    public float tocDoXoayDongXu = 120f;

    [Header("Camera xem trận")]
    [Tooltip("Bật: xem đúng camera third person của người đang sống (đồng bộ qua mạng). Tắt: camera đuổi sau xe.")]
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
    public GameObject eliminatePanelObject;
    public GameObject tutorialPanelObject;
    public GameObject pausePanelObject;
    public TMP_Text readyStatusText;
    public TMP_Text coinText;

    [Header("Panel khác (settings, shop...) - chỉ 1 panel hiện cùng lúc")]
    public GameObject[] panelKhac;

    [Header("HUD thời gian (đồng hồ tròn tự vẽ)")]
    public bool dungHudThoiGianMoi = true;
    [Tooltip("Kéo thanh xanh thời gian CŨ vào đây để ẩn nó. Để trống: tự tìm Slider có tên chứa time / thoi.")]
    public GameObject thanhThoiGianCu;
    public float kichThuocDongHo = 140f;

    [Header("Âm thanh & Nhạc nền")]
    public AudioClip nhacNenDuaXe;
    public AudioClip amThanhCheckPoint;
    public AudioClip amThanhWin;
    public AudioClip amThanhGameOver;
    [Range(0f, 1f)] public float amLuongNhacNen = 0.4f;

    [Header("Tắt nhạc")]
    public KeyCode phimTatNhac = KeyCode.M;
    [Tooltip("Tự tạo nút bật/tắt nhạc (icon loa). Tắt đi nếu bạn tự làm nút (gọi BatTatNhac()).")]
    public bool taoNutNhacTuDong = true;
    [Tooltip("Lệch so với góc trên phải. Tăng số âm theo X nếu bị đè minimap.")]
    public Vector2 viTriNutNhac = new Vector2(-360f, -20f);
    public float kichThuocNutNhac = 48f;

    private const string KEY_READY = "Ready";
    private const string KEY_ELIMINATED = "Eliminated";
    private const string KEY_SO_NGUOI = "SoNguoi";
    private const string KEY_NHAC_TAT = "NhacBiTat";

    private bool cheDoNhieuNguoi = false;
    private bool LaCheDoNhieuNguoi { get { return cheDoNhieuNguoi; } }

    private float thoiGianCoTheQuaLai = 0f;
    private float thoiGianCoTheBiTruNuoc = 0f;
    private float thoiDiemBatDauDua = -1f;
    private bool isPaused = false;
    private bool banThanDaBam = false;
    private bool daCoNguoiThang = false;
    private bool daDuyetThang = false;
    private bool daNhanThuongTranNay = false;
    private bool daXoaXe = false;
    private int soXuHienThiCuoi = -1;
    private int actorNguoiThang = -1;

    // Tắt nhạc
    private bool nhacBiTat = false;
    private GameObject nutNhac;
    private Image nutNhacIcon;
    private static Sprite spriteLoaBat, spriteLoaTat;

    // Chỉ 1 panel hiện cùng lúc
    private readonly List<GameObject> dsPanel = new List<GameObject>();
    private GameObject panelDangHien;

    // HUD thời gian tự vẽ
    private GameObject hudRoot;
    private TMP_Text hudSo;
    private Image hudFillImg;
    private float hudToiDa = 30f;
    private float hudFillHienTai = 1f;
    private bool hudDaTao = false;
    private static Sprite spriteBoTron, spriteDia, spriteVong, spriteXuVang;
    private Image hudVien;
    private float hudFlashDen = 0f;

    // Banner thông báo, phủ màn hình kết quả
    private GameObject bannerRoot;
    private Image bannerVach;
    private TMP_Text bannerTieuDe;
    private TMP_Text bannerPhu;
    private Image phuMan;
    private Coroutine coroutinePhuMan;

    // Camera khi thua (quay quanh chỗ xe bị loại)
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
    private GameObject congDichCache;
    private XuLyVaChamChoXe xeCuaMinhCache;
    private float thoiDiemTimXeTiepTheo = 0f;

    private AudioSource nhacNenSource;
    private AudioSource hieuUngSource;

    private Coroutine coroutineSuKien;

    // ---- Xem trận ----
    private bool dangXemTran = false;
    private bool dangChoKetQua = false;
    private Camera camXem;
    private Transform mucTieuXem;
    private float thoiDiemQuetXeTiepTheo = 0f;
    private float thoiDiemLamMoiThanh = 0f;

    private GameObject thanhXem;
    private TMP_Text tenXemText;
    private GameObject nutTruoc;
    private GameObject nutSau;

    // ---- Chia sẻ camera third person qua mạng ----
    private class GocCam { public Vector3 pos; public Quaternion rot = Quaternion.identity; public float time; }
    private readonly Dictionary<int, GocCam> gocCamNguoiKhac = new Dictionary<int, GocCam>();
    private float thoiDiemGuiCam = 0f;
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

        danhSachDongXu = GameObject.FindGameObjectsWithTag("CheckPoint");

        if (resultPanelObject == null) resultPanelObject = TimObjectTrongCanvas("result");
        if (eliminatePanelObject == null) eliminatePanelObject = TimObjectTrongCanvas("eliminate");

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

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { KEY_READY, false },
                { KEY_ELIMINATED, false }
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

    private void KhoiPhucTutorialNeuCan()
    {
        if (!daBatDauDua && !banThanDaBam && tutorialPanelObject != null)
            tutorialPanelObject.SetActive(true);
    }

    // =========================================================================
    // TẮT / BẬT NHẠC (ICON LOA)
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

        Image nen = TaoAnhCon("NutNhac", canvas.transform, new Color(0.04f, 0.07f, 0.12f, 0.85f));
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

        scooterCuaMinh = null;
        foreach (var sc in FindObjectsByType<scooter>(FindObjectsSortMode.None))
        {
            PhotonView pv = sc.GetComponentInParent<PhotonView>();
            if (!PhotonNetwork.IsConnected || pv == null || pv.IsMine)
            {
                scooterCuaMinh = sc.transform;
                break;
            }
        }
        return scooterCuaMinh;
    }

    private void XoaXeVaDoCuaMinh()
    {
        if (daXoaXe) return;

        GameObject xeObj = null;
        if (xeCuaMinhCache != null)
        {
            xeObj = xeCuaMinhCache.gameObject;
        }
        else
        {
            foreach (var sc in FindObjectsByType<scooter>(FindObjectsSortMode.None))
            {
                PhotonView pv = sc.GetComponentInParent<PhotonView>();
                if (!PhotonNetwork.IsConnected || pv == null || pv.IsMine)
                {
                    xeObj = sc.gameObject;
                    break;
                }
            }
        }

        if (xeObj == null) return;
        daXoaXe = true;

        viTriLucThua = xeObj.transform.position;
        coViTriLucThua = true;

        Transform rootXe = xeObj.transform;
        while (rootXe.parent != null && rootXe.parent.GetComponent<PhotonView>() != null)
            rootXe = rootXe.parent;
        GameObject targetDestroy = rootXe.gameObject;

        foreach (Camera c in targetDestroy.GetComponentsInChildren<Camera>(true))
            c.transform.SetParent(null, true);

        foreach (Collider col in targetDestroy.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        Rigidbody rb = targetDestroy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        PhotonView pvXe = targetDestroy.GetComponent<PhotonView>();
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && pvXe != null && pvXe.IsMine)
            PhotonNetwork.Destroy(targetDestroy);
        else
            targetDestroy.SetActive(false);

        xeCuaMinhCache = null;
        scooterCuaMinh = null;
    }

    // =========================================================================
    // CHIA SẺ CAMERA THIRD PERSON
    // =========================================================================
    private bool CoNguoiDaBiLoai()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
            if (DaBiLoai(p)) return true;
        return false;
    }

    private void GuiGocCamera()
    {
        if (!cheDoNhieuNguoi || !PhotonNetwork.InRoom) return;
        if (!daBatDauDua || daBiLoai || ketThucGame || winGame) return;
        if (Time.unscaledTime < thoiDiemGuiCam) return;
        thoiDiemGuiCam = Time.unscaledTime + 0.1f;

        if (!CoNguoiDaBiLoai()) return;

        Camera cam = LayMainCam();
        Transform xe = LayScooterCuaMinh();
        if (cam == null || xe == null) return;

        Quaternion yaw = Quaternion.Euler(0f, xe.eulerAngles.y, 0f);
        Quaternion nghich = Quaternion.Inverse(yaw);
        Vector3 offset = nghich * (cam.transform.position - xe.position);
        Quaternion rot = nghich * cam.transform.rotation;

        photonView.RPC(nameof(NhanGocCamera), RpcTarget.Others, offset, rot);
    }

    [PunRPC]
    private void NhanGocCamera(Vector3 offset, Quaternion rot, PhotonMessageInfo info)
    {
        if (info.Sender == null) return;

        int actor = info.Sender.ActorNumber;
        GocCam g;
        if (!gocCamNguoiKhac.TryGetValue(actor, out g))
        {
            g = new GocCam();
            gocCamNguoiKhac[actor] = g;
        }
        g.pos = offset;
        g.rot = rot;
        g.time = Time.unscaledTime;
    }

    // =========================================================================
    // XEM TRẬN
    // =========================================================================
    private int ActorCuaXe(Transform t)
    {
        PhotonView pv = t.GetComponentInParent<PhotonView>();
        return pv != null ? pv.OwnerActorNr : 0;
    }

    private List<Transform> LayXeNguoiKhac()
    {
        var ds = new List<Transform>();
        foreach (var sc in FindObjectsByType<scooter>(FindObjectsSortMode.None))
        {
            if (!sc.gameObject.activeInHierarchy) continue;
            PhotonView pv = sc.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine) continue;
            ds.Add(sc.transform);
        }
        ds.Sort((a, b) => ActorCuaXe(a).CompareTo(ActorCuaXe(b)));
        return ds;
    }

    private void BatDauXemTran()
    {
        if (dangXemTran) return;

        camXem = LayMainCam() != null ? LayMainCam() : FindFirstObjectByType<Camera>();
        if (camXem == null) return;

        foreach (MonoBehaviour mb in camXem.GetComponents<MonoBehaviour>())
            mb.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (coinText != null) coinText.gameObject.SetActive(false);

        dangXemTran = true;
        ChonMucTieuXem(0);
    }

    private void ChonMucTieuXem(int huong)
    {
        var ds = LayXeNguoiKhac();
        if (ds.Count == 0) { mucTieuXem = null; CapNhatXemText(); return; }

        int idx = ds.IndexOf(mucTieuXem);
        idx = (idx < 0) ? 0 : (idx + huong + ds.Count) % ds.Count;
        mucTieuXem = ds[idx];
        CapNhatXemText();
    }

    private void ChonXeTheoActor(int actor)
    {
        foreach (var t in LayXeNguoiKhac())
        {
            if (ActorCuaXe(t) == actor)
            {
                mucTieuXem = t;
                return;
            }
        }
    }

    private void TaoThanhXem()
    {
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        thanhXem = new GameObject("ThanhXemTran", typeof(RectTransform), typeof(Image));
        thanhXem.transform.SetParent(canvas.transform, false);

        RectTransform rect = thanhXem.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 30f);
        rect.sizeDelta = new Vector2(560f, 76f);

        thanhXem.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject txtObj = new GameObject("TenNguoiXem", typeof(RectTransform));
        txtObj.transform.SetParent(thanhXem.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(80f, 0f);
        tr.offsetMax = new Vector2(-80f, 0f);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tenXemText = tmp;

        nutTruoc = TaoNutXem(thanhXem.transform, "<", true, -1);
        nutSau = TaoNutXem(thanhXem.transform, ">", false, 1);

        thanhXem.transform.SetAsLastSibling();
    }

    private GameObject TaoNutXem(Transform cha, string nhan, bool ben_trai, int huong)
    {
        GameObject o = new GameObject("Nut" + (ben_trai ? "Truoc" : "Sau"), typeof(RectTransform), typeof(Image), typeof(Button));
        o.transform.SetParent(cha, false);

        RectTransform r = o.GetComponent<RectTransform>();
        float x = ben_trai ? 0f : 1f;
        r.anchorMin = r.anchorMax = new Vector2(x, 0.5f);
        r.pivot = new Vector2(x, 0.5f);
        r.anchoredPosition = new Vector2(ben_trai ? 8f : -8f, 0f);
        r.sizeDelta = new Vector2(62f, 62f);

        Image img = o.GetComponent<Image>();
        img.color = new Color(0.1f, 0.7f, 0.9f, 0.95f);

        Button btn = o.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            if (dangChoKetQua) return;
            XoaTieuDiemUI();
            ChonMucTieuXem(huong);
        });

        GameObject txtObj = new GameObject("Label", typeof(RectTransform));
        txtObj.transform.SetParent(o.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = nhan;
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return o;
    }

    private void CapNhatXemText()
    {
        if (!dangXemTran) return;

        if (thanhXem == null) TaoThanhXem();
        if (thanhXem == null) return;

        bool bangThoatDangMo = eliminatePanelObject != null && eliminatePanelObject.activeSelf;
        thanhXem.SetActive(!bangThoatDangMo);
        if (bangThoatDangMo) return;

        var ds = LayXeNguoiKhac();
        int idx = mucTieuXem != null ? ds.IndexOf(mucTieuXem) : -1;

        string ten = null;
        if (mucTieuXem != null)
        {
            PhotonView pv = mucTieuXem.GetComponentInParent<PhotonView>();
            if (pv != null && pv.Owner != null) ten = LayTenNguoiChoi(pv.Owner);
        }

        bool coTheDoi = !dangChoKetQua && ds.Count > 1;
        if (nutTruoc != null) nutTruoc.SetActive(coTheDoi);
        if (nutSau != null) nutSau.SetActive(coTheDoi);

        if (tenXemText == null) return;

        if (ten == null)
            tenXemText.text = "Không còn ai để xem";
        else if (dangChoKetQua)
            tenXemText.text = $"NGƯỜI THẮNG\n<b>{ten}</b>";
        else
            tenXemText.text = $"<b>{ten}</b>  ({idx + 1}/{ds.Count})\n<size=60%><color=#BBBBBB>Q / E đổi người · ESC menu</color></size>";
    }

    private void AnXemText()
    {
        if (thanhXem != null) thanhXem.SetActive(false);
    }

    private void LateUpdate()
    {
        if (dangQuanhXe && camQuanh != null) QuanhDiemChet(camQuanh);

        if (!dangXemTran || camXem == null) return;

        if (!dangChoKetQua)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Tab)) ChonMucTieuXem(1);
            else if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow)) ChonMucTieuXem(-1);
        }

        if (Time.unscaledTime >= thoiDiemLamMoiThanh)
        {
            thoiDiemLamMoiThanh = Time.unscaledTime + 0.5f;
            CapNhatXemText();
        }

        if (mucTieuXem == null || !mucTieuXem.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= thoiDiemQuetXeTiepTheo)
            {
                thoiDiemQuetXeTiepTheo = Time.unscaledTime + 0.5f;
                mucTieuXem = null;
                if (dangChoKetQua && actorNguoiThang > 0) ChonXeTheoActor(actorNguoiThang);
                if (mucTieuXem == null) ChonMucTieuXem(0);
                else CapNhatXemText();
            }
            if (mucTieuXem == null)
            {
                if (coViTriLucThua) QuanhDiemChet(camXem);
                return;
            }
        }

        int actor = ActorCuaXe(mucTieuXem);
        GocCam g;
        bool coCamThat = xemTheoCameraNguoiChoi
                         && gocCamNguoiKhac.TryGetValue(actor, out g)
                         && Time.unscaledTime - g.time < 1f;

        if (coCamThat)
        {
            g = gocCamNguoiKhac[actor];
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
            coinText.text = $"XU: {tongXu}";
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

        foreach (TMP_Text txt in panel.GetComponentsInChildren<TMP_Text>(true))
            if (LaTextTongXu(txt.gameObject.name.ToLower()))
                txt.text = $"TỔNG XU: {tongXu}";
    }

    private void XoaTieuDiemUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // =========================================================================
    // BẮT ĐẦU ĐUA & SẴN SÀNG
    // =========================================================================
    private void BatNhacNen()
    {
        if (nhacNenSource != null && nhacNenDuaXe != null && !nhacNenSource.isPlaying)
        {
            nhacNenSource.clip = nhacNenDuaXe;
            nhacNenSource.volume = amLuongNhacNen;
            nhacNenSource.mute = nhacBiTat;
            nhacNenSource.Play();
        }
    }

    private void BatDauDua()
    {
        if (daBatDauDua) return;

        cheDoNhieuNguoi = cheDoNhieuNguoi && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1;

        daBatDauDua = true;
        thoiDiemBatDauDua = Time.time;
        Time.timeScale = 1f;

        XoaTieuDiemUI();

        if (timeGameObject != null) timeGameObject.SetActive(true);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
        HienThiThoiGianGame();
        CapNhatHienThiXu(true);
        BatNhacNen();
    }

    public void BatDauGame()
    {
        if (banThanDaBam) return;
        banThanDaBam = true;

        XoaTieuDiemUI();
        if (tutorialPanelObject != null) tutorialPanelObject.SetActive(false);

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { KEY_READY, true } });
            KiemTraSanSang();
        }
        else
        {
            BatDauDua();
        }
    }

    private void KiemTraSanSang()
    {
        if (daBatDauDua || !cheDoNhieuNguoi || !PhotonNetwork.InRoom) return;

        int tong = PhotonNetwork.CurrentRoom.PlayerCount;
        int daSanSang = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(KEY_READY, out object v) && v is bool b && b)
                daSanSang++;
        }

        if (daSanSang >= tong)
        {
            BatDauDua();
            return;
        }

        if (readyStatusText == null) return;
        if (!banThanDaBam && (tutorialPanelObject == null || !tutorialPanelObject.activeInHierarchy)) return;

        readyStatusText.gameObject.SetActive(true);

        if (tong <= 1)
        {
            readyStatusText.text = "Tất cả đối thủ đã thoát phòng.\nBấm BẮT ĐẦU ĐUA để chạy một mình.";
        }
        else
        {
            string dong2 = banThanDaBam
                ? "Bạn đã sẵn sàng - đang chờ những người còn lại..."
                : "Bấm BẮT ĐẦU ĐUA khi bạn đã sẵn sàng!";
            readyStatusText.text = $"Trong phòng: {tong} người | Đã sẵn sàng: {daSanSang}/{tong}\n{dong2}";
        }
    }

    private void DatLaiSanSang()
    {
        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { KEY_READY, false } });
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(KEY_SO_NGUOI) && !daBatDauDua)
        {
            XacDinhCheDo();
            KiemTraSanSang();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!cheDoNhieuNguoi) return;

        if (changedProps.ContainsKey(KEY_READY)) KiemTraSanSang();

        if (changedProps.ContainsKey(KEY_ELIMINATED))
        {
            KiemTraNguoiSotLaiCuoiCung();
        }
    }

    // =========================================================================
    // NGƯỜI CHƠI THOÁT / MẤT KẾT NỐI
    // =========================================================================
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!cheDoNhieuNguoi) return;

        gocCamNguoiKhac.Remove(otherPlayer.ActorNumber);

        KiemTraSanSang();
        KiemTraNguoiSotLaiCuoiCung(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("menu");
    }

    private string LayTenNguoiChoi(Player p)
    {
        if (p == null) return "Người chơi";
        return string.IsNullOrEmpty(p.NickName) ? $"Người chơi #{p.ActorNumber}" : p.NickName;
    }

    private bool DaBiLoai(Player p)
    {
        if (p.IsLocal && daBiLoai) return true;
        return p.CustomProperties.TryGetValue(KEY_ELIMINATED, out object v) && v is bool b && b;
    }

    // =========================================================================
    // BANNER & POPUP +GIÂY (ĐỒNG XU VÀNG)
    // =========================================================================
    private void HienThongBaoSuKien(string noiDung, Color mauChu, float thoiGianHien = 2.2f)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return;
        if (CoBangDangMo()) return;
        if (!TaoBanner()) return;

        string[] phan = noiDung.Split(new[] { '\n' }, 2);
        string tieuDe = phan[0].Trim();
        string phu = phan.Length > 1 ? phan[1].Trim().Replace("\n", "  ·  ") : "";

        if (coroutineSuKien != null) StopCoroutine(coroutineSuKien);
        coroutineSuKien = StartCoroutine(ChayBanner(tieuDe, phu, mauChu, thoiGianHien));
    }

    private bool TaoBanner()
    {
        if (bannerRoot != null) return true;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return false;

        bannerRoot = new GameObject("BannerThongBao", typeof(RectTransform), typeof(CanvasGroup));
        bannerRoot.transform.SetParent(canvas.transform, false);
        RectTransform rr = (RectTransform)bannerRoot.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.72f);
        rr.pivot = new Vector2(0.5f, 0.5f);
        rr.anchoredPosition = Vector2.zero;
        rr.sizeDelta = new Vector2(920f, 128f);

        CanvasGroup cg = bannerRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image nen = TaoAnhCon("Nen", bannerRoot.transform, new Color(0.04f, 0.06f, 0.1f, 0.82f));
        Stretch(nen.rectTransform);

        bannerVach = TaoAnhCon("Vach", bannerRoot.transform, Color.white);
        RectTransform rv = bannerVach.rectTransform;
        rv.anchorMin = new Vector2(0f, 0f);
        rv.anchorMax = new Vector2(0f, 1f);
        rv.pivot = new Vector2(0f, 0.5f);
        rv.anchoredPosition = new Vector2(14f, 0f);
        rv.sizeDelta = new Vector2(8f, -28f);

        bannerTieuDe = TaoChuCon("TieuDe", bannerRoot.transform, 44f, Color.white, true);
        bannerTieuDe.enableAutoSizing = true;
        bannerTieuDe.fontSizeMin = 22f;
        bannerTieuDe.fontSizeMax = 44f;
        bannerTieuDe.textWrappingMode = TextWrappingModes.NoWrap;

        bannerPhu = TaoChuCon("Phu", bannerRoot.transform, 26f, new Color(0.85f, 0.9f, 0.95f, 1f), false);
        bannerPhu.fontStyle = FontStyles.Normal;
        bannerPhu.enableAutoSizing = true;
        bannerPhu.fontSizeMin = 14f;
        bannerPhu.fontSizeMax = 26f;
        bannerPhu.textWrappingMode = TextWrappingModes.NoWrap;

        bannerRoot.SetActive(false);
        return true;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void DatKhung(RectTransform r, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
    {
        r.anchorMin = amin;
        r.anchorMax = amax;
        r.offsetMin = omin;
        r.offsetMax = omax;
    }

    private IEnumerator ChayBanner(string tieuDe, string phu, Color mau, float thoiGian)
    {
        bool coPhu = !string.IsNullOrEmpty(phu);

        if (coPhu)
        {
            DatKhung(bannerTieuDe.rectTransform, new Vector2(0f, 0.42f), Vector2.one, new Vector2(44f, 0f), new Vector2(-28f, -10f));
            DatKhung(bannerPhu.rectTransform, Vector2.zero, new Vector2(1f, 0.42f), new Vector2(44f, 10f), new Vector2(-28f, 0f));
        }
        else
        {
            DatKhung(bannerTieuDe.rectTransform, Vector2.zero, Vector2.one, new Vector2(44f, 6f), new Vector2(-28f, -6f));
        }
        bannerPhu.gameObject.SetActive(coPhu);

        bannerTieuDe.text = tieuDe;
        bannerTieuDe.color = mau;
        bannerPhu.text = phu;
        bannerVach.color = mau;

        CanvasGroup cg = bannerRoot.GetComponent<CanvasGroup>();
        RectTransform rr = (RectTransform)bannerRoot.transform;
        bannerRoot.transform.SetAsLastSibling();
        bannerRoot.SetActive(true);

        const float tVao = 0.28f;
        for (float t = 0f; t < tVao; t += Time.unscaledDeltaTime)
        {
            float x = t / tVao - 1f;
            float nay = 1f + 2.70158f * x * x * x + 1.70158f * x * x;
            rr.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, nay);
            cg.alpha = Mathf.Clamp01(t / tVao * 1.5f);
            yield return null;
        }
        rr.localScale = Vector3.one;
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.3f, thoiGian));

        const float tRa = 0.35f;
        for (float t = 0f; t < tRa; t += Time.unscaledDeltaTime)
        {
            cg.alpha = 1f - t / tRa;
            rr.localScale = Vector3.one * Mathf.Lerp(1f, 0.96f, t / tRa);
            yield return null;
        }
        bannerRoot.SetActive(false);
    }

    private void HienPopupThoiGian(string chu, Color mau)
    {
        if (CoBangDangMo()) return;
        Canvas canvas = LayCanvasChinh();
        if (canvas == null) return;

        if (spriteXuVang == null) spriteXuVang = TaoSpriteXuVang();

        Image nen = TaoAnhCon("PopupXuThoiGian", canvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.9f));
        RectTransform r = nen.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(kichThuocDongHo * 0.5f + 115f, -40f);
        r.sizeDelta = new Vector2(190f, 66f);

        // Viền vàng mảnh
        Image vien = TaoAnhCon("Vien", nen.transform, new Color(1f, 0.82f, 0.2f, 0.9f));
        vien.transform.SetAsFirstSibling();
        DatKhung(vien.rectTransform, Vector2.zero, Vector2.one, new Vector2(-2f, -2f), new Vector2(2f, 2f));

        // Đồng xu vàng
        GameObject coinObj = new GameObject("DongXuVang", typeof(RectTransform), typeof(Image));
        coinObj.transform.SetParent(nen.transform, false);
        Image coin = coinObj.GetComponent<Image>();
        coin.sprite = spriteXuVang;
        coin.preserveAspect = true;
        coin.raycastTarget = false;
        RectTransform rc = coin.rectTransform;
        rc.anchorMin = rc.anchorMax = new Vector2(0f, 0.5f);
        rc.pivot = new Vector2(0.5f, 0.5f);
        rc.anchoredPosition = new Vector2(40f, 0f);
        rc.sizeDelta = new Vector2(52f, 52f);

        TMP_Text t = TaoChuCon("Chu", nen.transform, 42f, mau, true);
        t.alignment = TextAlignmentOptions.Left;
        DatKhung(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(78f, 0f), new Vector2(-8f, 0f));
        t.text = chu;

        CanvasGroup cg = nen.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        StartCoroutine(ChayPopup(nen.gameObject, r, cg, rc));
    }

    private IEnumerator ChayPopup(GameObject go, RectTransform r, CanvasGroup cg, RectTransform coin)
    {
        Vector2 batDau = r.anchoredPosition;
        const float tong = 1.6f;

        for (float t = 0f; t < tong; t += Time.unscaledDeltaTime)
        {
            if (go == null) yield break;
            float x = t / tong;

            float pop = x < 0.12f ? Mathf.Lerp(0.4f, 1.2f, x / 0.12f)
                                  : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((x - 0.12f) / 0.12f));
            r.localScale = Vector3.one * pop;
            r.anchoredPosition = batDau + Vector2.up * (-10f + 30f * x);
            cg.alpha = x < 0.75f ? 1f : 1f - (x - 0.75f) / 0.25f;

            // Đồng xu xoay
            if (coin != null)
            {
                float xoay = x < 0.6f ? Mathf.Cos(t * 12f) : 1f;
                coin.localScale = new Vector3(xoay, 1f, 1f);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
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
        t.text = $"+{xuNhanDuoc} XU";
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    private void Update()
    {
        CapNhatHienThiXu();
        GuiGocCamera();
        CapNhatHudThoiGian();
        DamBaoMotPanel();

        if (Input.GetKeyDown(phimTatNhac)) BatTatNhac();

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

        if (Input.GetKeyDown(KeyCode.Escape)) XuLyEscNgoaiTranDua();

        if (Input.GetKeyDown(KeyCode.Escape) && daBatDauDua && !ketThucGame && !winGame && !dangChoKetQua)
        {
            if (daBiLoai)
            {
                if (dangXemTran && eliminatePanelObject != null)
                {
                    bool bat = !eliminatePanelObject.activeSelf;
                    eliminatePanelObject.SetActive(bat);
                    if (bat) eliminatePanelObject.transform.SetAsLastSibling();
                    CapNhatXemText();
                }
            }
            else if (isPaused) ResumeGame();
            else PauseGame();
        }

        bool dangDua = daBatDauDua && !daBiLoai && !ketThucGame && !winGame;

        if (dangDua && (cheDoNhieuNguoi || !isPaused))
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

    // trong = 0: đĩa đặc. trong > 0: vòng (tỉ lệ bán kính trong / ngoài)
    private static Sprite TaoSpriteTron(float trong)
    {
        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float c = (S - 1) / 2f;
        float R = S / 2f - 1f;
        float r0 = R * trong;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(R - d + 0.5f);
                if (trong > 0f) a *= Mathf.Clamp01(d - r0 + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite TaoSpriteXuVang()
    {
        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float c = (S - 1) / 2f;
        float R = S / 2f - 1f;
        Vector2 sang = new Vector2(c - 22f, c + 24f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, new Vector2(c, c));
                float a = Mathf.Clamp01(R - d + 0.5f);
                float t = d / R;

                Color col;
                if (t > 0.86f) col = new Color(0.68f, 0.42f, 0.04f);
                else if (t > 0.78f) col = new Color(1f, 0.94f, 0.5f);
                else col = Color.Lerp(new Color(1f, 0.86f, 0.22f), new Color(0.96f, 0.68f, 0.1f), t / 0.78f);

                if (t > 0.5f && t < 0.57f) col = new Color(0.82f, 0.56f, 0.08f);

                float dl = Vector2.Distance(p, sang);
                if (dl < 16f && t < 0.78f)
                    col = Color.Lerp(col, Color.white, 0.55f * (1f - dl / 16f));

                col.a = a;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
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

    private static Image TaoAnhTron(string ten, Transform cha, Sprite sprite, Color mau)
    {
        GameObject o = new GameObject(ten, typeof(RectTransform), typeof(Image));
        o.transform.SetParent(cha, false);
        Image img = o.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
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
    // HUD THỜI GIAN (ĐỒNG HỒ TRÒN)
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

        if (thanhThoiGianCu != null)
        {
            AnThanhCu(thanhThoiGianCu);
        }
        else
        {
            foreach (Slider s in canvas.GetComponentsInChildren<Slider>(true))
            {
                string ten = s.gameObject.name.ToLower();
                if (ten.Contains("time") || ten.Contains("thoi"))
                    AnThanhCu(s.gameObject);
            }
        }

        if (spriteDia == null) spriteDia = TaoSpriteTron(0f);
        if (spriteVong == null) spriteVong = TaoSpriteTron(0.84f);

        float kt = kichThuocDongHo;

        hudRoot = new GameObject("HudThoiGian", typeof(RectTransform));
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform rr = (RectTransform)hudRoot.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f);
        rr.pivot = new Vector2(0.5f, 1f);
        rr.anchoredPosition = new Vector2(0f, -16f);
        rr.sizeDelta = new Vector2(kt, kt);

        // Quầng sáng (nhấp nháy khi sắp hết giờ / vừa cộng giờ)
        hudVien = TaoAnhTron("Quang", hudRoot.transform, spriteDia, new Color(0.3f, 0.9f, 0.45f, 0.35f));
        DatKhung(hudVien.rectTransform, Vector2.zero, Vector2.one, new Vector2(-8f, -8f), new Vector2(8f, 8f));

        // Nền đồng hồ
        Image nen = TaoAnhTron("Nen", hudRoot.transform, spriteDia, new Color(0.04f, 0.07f, 0.12f, 0.94f));
        Stretch(nen.rectTransform);

        // Đường ray của vòng
        Image ray = TaoAnhTron("VongNen", hudRoot.transform, spriteVong, new Color(1f, 1f, 1f, 0.14f));
        DatKhung(ray.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        // Vòng thời gian còn lại
        hudFillImg = TaoAnhTron("VongFill", hudRoot.transform, spriteVong, new Color(0.3f, 0.9f, 0.45f, 1f));
        DatKhung(hudFillImg.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        hudFillImg.type = Image.Type.Filled;
        hudFillImg.fillMethod = Image.FillMethod.Radial360;
        hudFillImg.fillOrigin = (int)Image.Origin360.Top;
        hudFillImg.fillClockwise = true;
        hudFillImg.fillAmount = 1f;

        // Số giây
        hudSo = TaoChuCon("So", hudRoot.transform, kt * 0.42f, Color.white, true);
        RectTransform rs = hudSo.rectTransform;
        rs.anchorMin = rs.anchorMax = new Vector2(0.5f, 0.5f);
        rs.pivot = new Vector2(0.5f, 0.5f);
        rs.anchoredPosition = new Vector2(0f, kt * 0.06f);
        rs.sizeDelta = new Vector2(kt * 0.7f, kt * 0.5f);

        // Nhãn nhỏ
        TMP_Text nhan = TaoChuCon("Nhan", hudRoot.transform, kt * 0.12f, new Color(0.75f, 0.85f, 0.95f, 0.85f), false);
        nhan.text = "GIÂY";
        RectTransform rn = nhan.rectTransform;
        rn.anchorMin = rn.anchorMax = new Vector2(0.5f, 0.5f);
        rn.pivot = new Vector2(0.5f, 0.5f);
        rn.anchoredPosition = new Vector2(0f, -kt * 0.27f);
        rn.sizeDelta = new Vector2(kt * 0.5f, kt * 0.18f);

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
            hudFillImg.fillAmount = hudFillHienTai;
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
            hudSo.text = Mathf.CeilToInt(t).ToString();
            hudSo.color = dangNhay ? new Color(1f, 0.9f, 0.35f) : (sapHet ? mau : Color.white);

            float xung = 1f;
            if (dangNhay) xung = 1f + 0.25f * Mathf.Clamp01((hudFlashDen - Time.unscaledTime) / 0.5f);
            else if (sapHet && t > 0f) xung = 1f + 0.1f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            hudSo.rectTransform.localScale = Vector3.one * xung;
        }
    }

    // =========================================================================
    // PAUSE / RESUME / THOÁT
    // =========================================================================
    private void XuLyEscNgoaiTranDua()
    {
        if (!daBatDauDua)
        {
            if (pausePanelObject == null) return;

            bool bat = !pausePanelObject.activeSelf;
            pausePanelObject.SetActive(bat);
            isPaused = bat;
            if (bat)
            {
                pausePanelObject.transform.SetAsLastSibling();
                MoKhoaChuot();
            }
            else
            {
                KhoiPhucTutorialNeuCan();
            }
            return;
        }

        if (ketThucGame || winGame) QuitGame();
    }

    public void PauseGame()
    {
        if (!daBatDauDua || ketThucGame || winGame) return;

        isPaused = true;
        MoKhoaChuot();
        XoaTieuDiemUI();
        CapNhatHienThiXu(true);
        if (pausePanelObject != null) pausePanelObject.SetActive(true);
        if (nhacNenSource != null && nhacNenSource.isPlaying) nhacNenSource.Pause();
        if (!cheDoNhieuNguoi) Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        XoaTieuDiemUI();
        if (pausePanelObject != null) pausePanelObject.SetActive(false);
        if (nhacNenSource != null) nhacNenSource.UnPause();
        if (daBatDauDua) Time.timeScale = 1f;
        else KhoiPhucTutorialNeuCan();
    }

    public void ChonOLaiXem()
    {
        XoaTieuDiemUI();
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        BatDauXemTran();
    }

    public void ChonThoatTran()
    {
        QuitGame();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else SceneManager.LoadScene("menu");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("menu");
    }

    // =========================================================================
    // ĐỒNG HỒ
    // =========================================================================
    public void HienThiThoiGianGame()
    {
        if (timeTextCache == null && timeGameObject != null)
            timeTextCache = timeGameObject.GetComponent<TextMeshProUGUI>();
        if (timeTextCache == null) return;

        int giay = Mathf.FloorToInt(thoiGianChoPhepVeDich);
        if (giay == giayHienThiCuoi) return;
        giayHienThiCuoi = giay;
        timeTextCache.SetText("{0}", giay);
    }

    // =========================================================================
    // BỊ LOẠI & NGƯỜI SỐNG SÓT CUỐI CÙNG
    // =========================================================================
    private void PhatAmThanhThua()
    {
        if (hieuUngSource != null && amThanhGameOver != null)
            hieuUngSource.PlayOneShot(amThanhGameOver);
    }

    public void XuLyBanThanBiLoai()
    {
        if (daBiLoai || ketThucGame || winGame) return;
        daBiLoai = true;

        XoaXeVaDoCuaMinh();
        PhatAmThanhThua();

        if (timeGameObject != null) timeGameObject.SetActive(false);

        if (cheDoNhieuNguoi)
        {
            HienThongBaoSuKien("BẠN ĐÃ BỊ LOẠI", Color.yellow, 2f);

            BatDauXemTran();

            if (!dangXemTran)
            {
                if (eliminatePanelObject == null)
                    eliminatePanelObject = TimObjectTrongCanvas("eliminate");
                if (eliminatePanelObject != null)
                {
                    eliminatePanelObject.SetActive(true);
                    eliminatePanelObject.transform.SetAsLastSibling();
                }
            }

            if (PhotonNetwork.InRoom)
                PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { KEY_ELIMINATED, true } });
            KiemTraNguoiSotLaiCuoiCung();
        }
        else
        {
            HienKetQuaThua("BẠN ĐÃ HẾT THỜI GIAN VÀ THUA CUỘC!");
        }
    }

    private void KiemTraNguoiSotLaiCuoiCung(bool doCoNguoiRoiPhong = false)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
        if (ketThucGame || daCoNguoiThang || !daBatDauDua || !cheDoNhieuNguoi) return;
        if (daDuyetThang) return;

        int soNguoiConDua = 0;
        Player nguoiSotLai = null;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!DaBiLoai(p))
            {
                soNguoiConDua++;
                nguoiSotLai = p;
            }
        }

        if (soNguoiConDua == 1 && nguoiSotLai != null)
        {
            if (doCoNguoiRoiPhong || !tuDongThangKhiConMotNguoi) return;

            daDuyetThang = true;
            photonView.RPC(nameof(ThongBaoCoNguoiThang), RpcTarget.All,
                nguoiSotLai.ActorNumber, true);
        }
        else if (soNguoiConDua == 0)
        {
            daDuyetThang = true;
            photonView.RPC(nameof(ThongBaoTatCaBiLoai), RpcTarget.All);
        }
    }

    [PunRPC]
    private void ThongBaoTatCaBiLoai()
    {
        if (daCoNguoiThang || ketThucGame || winGame) return;
        daCoNguoiThang = true;
        HienKetQuaThua("TẤT CẢ NGƯỜI CHƠI ĐỀU BỊ LOẠI! KHÔNG AI GIÀNH CHIẾN THẮNG.");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        KiemTraNguoiSotLaiCuoiCung(true);
    }

    public void KetThucGame()
    {
        XuLyBanThanBiLoai();
    }

    public void QuaCheckPoint()
    {
        if (!daBatDauDua || daBiLoai || ketThucGame || winGame) return;
        if (Time.time < thoiGianCoTheQuaLai) return;
        thoiGianCoTheQuaLai = Time.time + 0.05f;

        thoiGianChoPhepVeDich += thoiGianHoiKhiQuaCheckPoint;
        HienThiThoiGianGame();

        if (hieuUngSource != null && amThanhCheckPoint != null)
            hieuUngSource.PlayOneShot(amThanhCheckPoint);

        HienPopupThoiGian($"+{Mathf.FloorToInt(thoiGianHoiKhiQuaCheckPoint)}s", new Color(1f, 0.86f, 0.25f));
        hudFlashDen = Time.unscaledTime + 0.5f;
    }

    public void ChamNuocTruThoiGian()
    {
        if (daBiLoai || ketThucGame || winGame || !daBatDauDua) return;

        if (Time.time < thoiGianCoTheBiTruNuoc) return;
        thoiGianCoTheBiTruNuoc = Time.time + 1.5f;

        thoiGianChoPhepVeDich -= thoiGianTruKhiChamNuoc;
        bool hetGio = thoiGianChoPhepVeDich <= 0f;
        if (hetGio) thoiGianChoPhepVeDich = 0f;

        HienThiThoiGianGame();

        HienThongBaoSuKien(
            $"-{Mathf.FloorToInt(thoiGianTruKhiChamNuoc)} GIÂY! (RƠI XUỐNG NƯỚC)",
            Color.red,
            hetGio ? 2.5f : 2f
        );

        if (hetGio) XuLyBanThanBiLoai();
    }

    // =========================================================================
    // THẮNG
    // =========================================================================
    public void QuaWinPoint()
    {
        QuaWinPoint(null);
    }

    public void QuaWinPoint(Collider vachDich)
    {
        if (!daBatDauDua || daBiLoai || ketThucGame || winGame) return;

        if (thoiDiemBatDauDua > 0f && Time.time - thoiDiemBatDauDua < 1.5f) return;

        if (vachDich == null)
        {
            if (congDichCache == null)
                congDichCache = GameObject.FindGameObjectWithTag("WinPoint");
            if (congDichCache != null) vachDich = congDichCache.GetComponent<Collider>();
        }

        XuLyVaChamChoXe xe = LayXeCuaMinh();

        if (xe != null && (vachDich != null || congDichCache != null))
        {
            Vector3 diemDich = vachDich != null
                ? vachDich.bounds.ClosestPoint(xe.transform.position)
                : congDichCache.transform.position;

            float kc = Vector3.Distance(xe.transform.position, diemDich);
            if (kc > 25f) return;
        }

        if (cheDoNhieuNguoi && PhotonNetwork.InRoom)
            photonView.RPC(nameof(YeuCauThang), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        else
            XuLyMinhThang("BẠN ĐÃ VỀ ĐÍCH!");
    }

    [PunRPC]
    private void YeuCauThang(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || daDuyetThang || daCoNguoiThang) return;

        Player nguoiYeuCau = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (nguoiYeuCau == null || DaBiLoai(nguoiYeuCau)) return;

        daDuyetThang = true;
        photonView.RPC(nameof(ThongBaoCoNguoiThang), RpcTarget.All, actorNumber, false);
    }

    [PunRPC]
    private void ThongBaoCoNguoiThang(int actorNumberNguoiThang, bool thangDoDoiThuBiLoai)
    {
        if (daCoNguoiThang || winGame || ketThucGame) return;

        Player pThang = PhotonNetwork.InRoom
            ? PhotonNetwork.CurrentRoom.GetPlayer(actorNumberNguoiThang)
            : null;

        if (pThang == null) return;
        if (!thangDoDoiThuBiLoai && DaBiLoai(pThang)) return;

        daCoNguoiThang = true;
        daDuyetThang = true;
        actorNguoiThang = actorNumberNguoiThang;

        string tenNguoiThang = LayTenNguoiChoi(pThang);

        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == actorNumberNguoiThang)
        {
            XuLyMinhThang(thangDoDoiThuBiLoai
                ? "BẠN LÀ NGƯỜI CUỐI CÙNG SỐNG SÓT VÀ CHIẾN THẮNG!"
                : "BẠN ĐÃ VỀ ĐÍCH ĐẦU TIÊN!");
        }
        else
        {
            HienKetQuaThua(thangDoDoiThuBiLoai
                ? $"{tenNguoiThang} LÀ NGƯỜI CUỐI CÙNG SỐNG SÓT VÀ CHIẾN THẮNG."
                : $"{tenNguoiThang} ĐÃ VỀ ĐÍCH TRƯỚC! BẠN ĐÃ THUA.");
        }
    }

    // =========================================================================
    // KẾT QUẢ
    // =========================================================================
    private void XuLyMinhThang(string tieuDe)
    {
        if (winGame || ketThucGame) return;
        winGame = true;
        ketThucGame = true;
        dangXemTran = false;
        AnXemText();
        DatLaiSanSang();

        int xuThuong = Mathf.Max(0, cheDoNhieuNguoi ? thuongNguoiThang : thuongChoiSolo);

        if (!daNhanThuongTranNay && xuThuong > 0)
        {
            daNhanThuongTranNay = true;
            PlayerInventory.AddCoins(xuThuong);
        }
        CapNhatHienThiXu(true);

        if (nhacNenSource != null) nhacNenSource.Stop();
        if (hieuUngSource != null && amThanhWin != null) hieuUngSource.PlayOneShot(amThanhWin);

        int tongXu = PlayerInventory.GetCoins();
        string noiDungKetQua = $"{tieuDe}\nTHƯỞNG: +{xuThuong} XU | TỔNG XU: {tongXu}";

        HienBangKetQua(true, noiDungKetQua, xuThuong, tongXu, Color.yellow);
    }

    private void HienKetQuaThua(string lyDoThua, bool daXemXong = false)
    {
        if (ketThucGame || winGame) return;
        if (dangChoKetQua && !daXemXong) return;

        if (!daXemXong && cheDoNhieuNguoi && actorNguoiThang > 0)
        {
            if (!daBiLoai) PhatAmThanhThua();
            daBiLoai = true;
            if (timeGameObject != null) timeGameObject.SetActive(false);
            XoaXeVaDoCuaMinh();
            BatDauXemTran();
            StartCoroutine(XemNguoiThangRoiHienKetQua(lyDoThua));
            return;
        }

        ketThucGame = true;
        winGame = false;
        dangXemTran = false;
        AnXemText();
        DatLaiSanSang();

        XoaXeVaDoCuaMinh();
        CapNhatHienThiXu(true);

        if (!cheDoNhieuNguoi) BatDauCameraQuanhXe();

        if (nhacNenSource != null) nhacNenSource.Stop();
        if (!daBiLoai) PhatAmThanhThua();

        int tongXu = PlayerInventory.GetCoins();
        string noiDungKetQua = $"{lyDoThua}\nTHƯỞNG: 0 XU | TỔNG XU: {tongXu}";

        HienBangKetQua(false, noiDungKetQua, 0, tongXu, Color.red);
    }

    private void HienBangKetQua(bool thang, string noiDung, int xuNhanDuoc, int tongXu, Color mauChuDuPhong)
    {
        if (timeGameObject != null) timeGameObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);

        AnBanner();
        MoKhoaChuot();
        HienPhuMan(thang ? new Color(1f, 0.82f, 0.2f, 0.22f) : new Color(0.75f, 0.08f, 0.08f, 0.35f));

        bool coBang;
        if (cheDoNhieuNguoi)
        {
            if (winGameObject != null) winGameObject.SetActive(false);
            if (gameOverObject != null) gameOverObject.SetActive(false);
            coBang = resultPanelObject != null;
            MoResultPanel(noiDung, thang, xuNhanDuoc, tongXu);
        }
        else
        {
            if (resultPanelObject != null) resultPanelObject.SetActive(false);
            GameObject bang = thang ? winGameObject : gameOverObject;
            coBang = bang != null;
            if (bang != null)
            {
                bang.SetActive(true);
                bang.transform.SetAsLastSibling();

                bool coOChuThuong = CapNhatChuThuongTrongBang(bang, thang ? xuNhanDuoc : 0);
                if (thang && xuNhanDuoc > 0 && !coOChuThuong) TaoChuThuongTrongBang(bang, xuNhanDuoc);
            }
        }

        if (!coBang) HienThongBaoSuKien(noiDung, mauChuDuPhong, 4f);
    }

    private bool CapNhatChuThuongTrongBang(GameObject bang, int xuNhanDuoc)
    {
        if (bang == null) return false;

        bool tim = false;
        foreach (TMP_Text txt in bang.GetComponentsInChildren<TMP_Text>(true))
        {
            string tenObj = txt.gameObject.name.ToLower();
            if (LaTextTongXu(tenObj)) continue;
            if (tenObj.Contains("coin") || tenObj.Contains("reward") || tenObj.Contains("xu"))
            {
                txt.text = xuNhanDuoc > 0 ? $"+{xuNhanDuoc} XU" : "0 XU";
                tim = true;
            }
        }
        return tim;
    }

    private void MoResultPanel(string noiDungKetQua, bool laNguoiThang, int xuNhanDuoc, int tongXu)
    {
        if (resultPanelObject == null) return;

        resultPanelObject.SetActive(true);
        resultPanelObject.transform.SetAsLastSibling();

        foreach (TMP_Text txt in resultPanelObject.GetComponentsInChildren<TMP_Text>(true))
        {
            string tenObj = txt.gameObject.name.ToLower();
            if (LaTextTongXu(tenObj))
                txt.text = $"TỔNG XU: {tongXu}";
            else if (tenObj.Contains("result") || tenObj.Contains("status") || tenObj.Contains("winner") || tenObj.Contains("title") || tenObj.Contains("message"))
                txt.text = noiDungKetQua;
            else if (tenObj.Contains("coin") || tenObj.Contains("reward") || tenObj.Contains("xu"))
                txt.text = laNguoiThang ? $"+{xuNhanDuoc} XU" : "0 XU";
        }
    }
}