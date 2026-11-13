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

    [Header("Hiệu ứng đồng xu CheckPoint")]
    public float tocDoXoayDongXu = 120f;

    [Header("Camera xem trận")]
    [Tooltip("Bật: xem đúng camera third person của người đang sống (đồng bộ qua mạng). Tắt: camera đuổi sau xe.")]
    public bool xemTheoCameraNguoiChoi = true;
    public float khoangCachSau = 7f;     // chỉ dùng khi chưa nhận được camera của người kia
    public float doCaoCamera = 3.5f;
    public float thoiGianXemNguoiThang = 5f;

    [Header("Trạng thái")]
    public bool ketThucGame = false;
    public bool winGame = false;
    public bool daBatDauDua = false;
    public bool daBiLoai = false;

    [Header("Giao diện UI")]
    public GameObject timeGameObject;
    public GameObject gameOverObject;       // Chỉ dùng khi Solo
    public GameObject winGameObject;        // Chỉ dùng khi Solo
    public GameObject resultPanelObject;    // Chỉ dùng khi Multiplayer
    public GameObject eliminatePanelObject; // Ở lại xem / Thoát (mở bằng ESC khi đang xem)
    public GameObject tutorialPanelObject;
    public GameObject pausePanelObject;
    public TMP_Text readyStatusText;
    public TMP_Text coinText;

    [Header("Âm thanh & Nhạc nền")]
    public AudioClip nhacNenDuaXe;
    public AudioClip amThanhCheckPoint;
    public AudioClip amThanhWin;
    public AudioClip amThanhGameOver;
    [Range(0f, 1f)] public float amLuongNhacNen = 0.4f;

    private const string KEY_READY = "Ready";
    private const string KEY_ELIMINATED = "Eliminated";
    private const string KEY_SO_NGUOI = "SoNguoi"; // do LobbyManager ghi lúc bấm START

    // Chế độ chốt từ Lobby (và khóa hẳn khi bắt đầu đua)
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

    private GameObject[] danhSachDongXu;
    private Canvas canvasCache;

    private TextMeshProUGUI timeTextCache;
    private int giayHienThiCuoi = -1;
    private GameObject congDichCache;
    private XuLyVaChamChoXe xeCuaMinhCache;
    private float thoiDiemTimXeTiepTheo = 0f;

    private AudioSource nhacNenSource;
    private AudioSource hieuUngSource;

    private TMP_Text thongBaoSuKienText;
    private Coroutine coroutineSuKien;

    // ---- Feed thông báo nhỏ (gom lại, tự mờ) ----
    private class DongThongBao { public string noiDung; public string mau; public int soLan; public float hetHan; }
    private readonly List<DongThongBao> feed = new List<DongThongBao>();
    private readonly System.Text.StringBuilder feedSb = new System.Text.StringBuilder();
    private TMP_Text feedText;
    private bool feedDangRong = true;
    private const int TOI_DA_DONG_FEED = 4;
    private const float THOI_GIAN_FEED = 4f;
    private const float THOI_GIAN_MO_FEED = 1f;

    // ---- Xem trận ----
    private bool dangXemTran = false;
    private bool dangChoKetQua = false;
    private Camera camXem;
    private Transform mucTieuXem;
    private float thoiDiemQuetXeTiepTheo = 0f;
    private float thoiDiemLamMoiThanh = 0f;

    // Thanh xem gọn ở dưới màn hình: [<]  Tên (1/3)  [>]
    private GameObject thanhXem;
    private TMP_Text tenXemText;
    private GameObject nutTruoc;
    private GameObject nutSau;

    // ---- Chia sẻ camera third person qua mạng ----
    // Mỗi người còn sống gửi vị trí/góc camera của mình (so với xe, chỉ tính yaw) cho người khác.
    // Người đã chết nhận về và đặt camera y hệt góc đó quanh xe của người được xem.
    private class GocCam { public Vector3 pos; public Quaternion rot = Quaternion.identity; public float time; }
    private readonly Dictionary<int, GocCam> gocCamNguoiKhac = new Dictionary<int, GocCam>();
    private float thoiDiemGuiCam = 0f;
    private Transform scooterCuaMinh;
    private float thoiDiemTimScooter = 0f;

    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    // =========================================================================
    // KHỞI TẠO
    // =========================================================================
    private void Awake()
    {
        if (instance == null) instance = this;

        Debug.developerConsoleVisible = false;
        Application.runInBackground = true;

        nhacNenSource = gameObject.AddComponent<AudioSource>();
        nhacNenSource.loop = true;
        nhacNenSource.playOnAwake = false;
        nhacNenSource.volume = amLuongNhacNen;

        hieuUngSource = gameObject.AddComponent<AudioSource>();
        hieuUngSource.playOnAwake = false;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (instance == this) instance = null;
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

    // Số người chơi được chốt từ Lobby; chạy thẳng scene trong Editor thì mặc định solo
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

        // Đang hiện bảng hướng dẫn thì ẩn đồng hồ để không đè lên bảng
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

            if (!cheDoNhieuNguoi) Time.timeScale = 0f; // solo: đứng yên cho tới khi bấm bắt đầu
            KiemTraSanSang();                          // solo thì hàm này không hiện gì
        }
        else
        {
            BatDauDua();
        }
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

    // Transform "scooter" của mình (cùng loại object với xe người khác để hệ tọa độ khớp nhau)
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

    // Xóa xe + nhân vật + phụ kiện của mình (biến mất trên toàn mạng)
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

        Transform rootXe = xeObj.transform;
        while (rootXe.parent != null && rootXe.parent.GetComponent<PhotonView>() != null)
            rootXe = rootXe.parent;
        GameObject targetDestroy = rootXe.gameObject;

        // Tách camera khỏi xe để không bị xóa theo
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

    // Người còn sống gửi góc camera của mình (10 lần/giây) - chỉ gửi khi đã có người bị loại
    private void GuiGocCamera()
    {
        if (!cheDoNhieuNguoi || !PhotonNetwork.InRoom) return;
        if (!daBatDauDua || daBiLoai || ketThucGame || winGame) return;
        if (Time.unscaledTime < thoiDiemGuiCam) return;
        thoiDiemGuiCam = Time.unscaledTime + 0.1f;

        if (!CoNguoiDaBiLoai()) return;

        Camera cam = Camera.main;
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

        camXem = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (camXem == null) return;

        // Tắt script camera cũ (đang bám xe mình đã bị xóa)
        foreach (MonoBehaviour mb in camXem.GetComponents<MonoBehaviour>())
            mb.enabled = false;

        // Mở khóa chuột để bấm được nút < >
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ẩn bớt UI không cần khi đang xem
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

    // ---------- Thanh xem gọn: [<]  Tên (1/3)  [>] ----------
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

        // Chữ ở giữa
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
        thanhXem.SetActive(true);

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
        if (!dangXemTran || camXem == null) return;

        if (!dangChoKetQua)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Tab)) ChonMucTieuXem(1);
            else if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow)) ChonMucTieuXem(-1);
        }

        // Có người thoát / bị loại thì cập nhật lại số người
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
            if (mucTieuXem == null) return;
        }

        int actor = ActorCuaXe(mucTieuXem);
        GocCam g;
        bool coCamThat = xemTheoCameraNguoiChoi
                         && gocCamNguoiKhac.TryGetValue(actor, out g)
                         && Time.unscaledTime - g.time < 1f;

        if (coCamThat)
        {
            // Đúng camera third person của người đang chơi (kể cả khi họ xoay camera)
            g = gocCamNguoiKhac[actor];
            Quaternion yaw = Quaternion.Euler(0f, mucTieuXem.eulerAngles.y, 0f);
            Vector3 viTriMong = mucTieuXem.position + yaw * g.pos;
            Quaternion xoayMong = yaw * g.rot;

            camXem.transform.position = Vector3.Lerp(camXem.transform.position, viTriMong, 12f * Time.unscaledDeltaTime);
            camXem.transform.rotation = Quaternion.Slerp(camXem.transform.rotation, xoayMong, 12f * Time.unscaledDeltaTime);
        }
        else
        {
            // Chưa nhận được camera của người kia: tạm đuổi sau xe
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
            coinText.gameObject.SetActive(!dangXemTran); // đang xem trận thì ẩn cho đỡ rối
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
    // BẮT ĐẦU ĐUA & SẴN SÀNG (chỉ áp dụng khi nhiều người)
    // =========================================================================
    private void BatNhacNen()
    {
        if (nhacNenSource != null && nhacNenDuaXe != null && !nhacNenSource.isPlaying)
        {
            nhacNenSource.clip = nhacNenDuaXe;
            nhacNenSource.volume = amLuongNhacNen;
            nhacNenSource.Play();
        }
    }

    private void BatDauDua()
    {
        if (daBatDauDua) return;

        // Khóa chế độ. Nếu mọi đối thủ đã thoát trước khi đua thì chạy như solo
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

    // Nút "BẮT ĐẦU ĐUA"
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
            BatDauDua(); // solo: chạy luôn, không chờ ai
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
            bool biLoai = changedProps[KEY_ELIMINATED] is bool b && b;
            if (biLoai && !targetPlayer.IsLocal && daBatDauDua && !ketThucGame)
                HienThongBaoTren($"{LayTenNguoiChoi(targetPlayer)} bị loại · còn {DemNguoiConDua()} người", "FF6666");

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

        string ten = LayTenNguoiChoi(otherPlayer);
        int conLai = daBatDauDua ? DemNguoiConDua() : PhotonNetwork.CurrentRoom.PlayerCount;
        HienThongBaoTren($"{ten} đã thoát · còn {conLai} người", "FFE066");

        KiemTraSanSang();
        KiemTraNguoiSotLaiCuoiCung();
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

    private int DemNguoiConDua()
    {
        if (!PhotonNetwork.InRoom) return 1;

        int dem = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
            if (!DaBiLoai(p)) dem++;
        return dem;
    }

    // =========================================================================
    // THÔNG BÁO
    // =========================================================================

    // Thông báo nhỏ ở góc trên trái: gom lại, tối đa 4 dòng, tự mờ dần
    private void HienThongBaoTren(string noiDung, string mau = "FFE066")
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return;

        DongThongBao e = feed.Find(x => x.noiDung == noiDung && x.mau == mau);
        if (e != null)
        {
            e.soLan++;
            feed.Remove(e);   // đưa xuống cuối
        }
        else
        {
            e = new DongThongBao { noiDung = noiDung, mau = mau, soLan = 1 };
            if (feed.Count >= TOI_DA_DONG_FEED) feed.RemoveAt(0);
        }
        e.hetHan = Time.unscaledTime + THOI_GIAN_FEED;
        feed.Add(e);
    }

    private void CapNhatFeed()
    {
        if (feed.Count == 0 && feedDangRong) return;

        feed.RemoveAll(e => Time.unscaledTime >= e.hetHan);

        if (feed.Count == 0)
        {
            if (feedText != null) feedText.text = "";
            feedDangRong = true;
            return;
        }

        if (feedText == null)
        {
            feedText = TaoText("FeedThongBaoUI", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -100f), new Vector2(650f, 180f), 24, Color.white,
                TextAlignmentOptions.TopLeft, true);
            if (feedText == null) return;
        }

        feedDangRong = false;
        feedSb.Clear();
        foreach (DongThongBao e in feed)
        {
            float conLai = e.hetHan - Time.unscaledTime;
            int alpha = conLai < THOI_GIAN_MO_FEED ? Mathf.RoundToInt(255f * conLai / THOI_GIAN_MO_FEED) : 255;
            feedSb.Append($"<color=#{e.mau}><alpha=#{alpha:X2}>{e.noiDung}");
            if (e.soLan > 1) feedSb.Append($" <b>(x{e.soLan})</b>");
            feedSb.Append("</color>\n");
        }
        feedText.text = feedSb.ToString();
    }

    // Thông báo lớn giữa màn hình: chỉ dùng cho việc quan trọng
    private void HienThongBaoSuKien(string noiDung, Color mauChu, float thoiGianHien = 2.2f)
    {
        if (thongBaoSuKienText == null)
        {
            thongBaoSuKienText = TaoText("ThongBaoSuKienUI", new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1100f, 90f), 42, Color.white,
                TextAlignmentOptions.Center, true);
            if (thongBaoSuKienText == null) return;
        }

        if (coroutineSuKien != null) StopCoroutine(coroutineSuKien);
        coroutineSuKien = StartCoroutine(ChayHienText(thongBaoSuKienText, noiDung, mauChu, thoiGianHien));
    }

    private IEnumerator ChayHienText(TMP_Text txt, string noiDung, Color mau, float thoiGian)
    {
        txt.transform.SetAsLastSibling();
        txt.gameObject.SetActive(true);
        txt.color = mau;
        txt.text = noiDung;

        yield return new WaitForSecondsRealtime(thoiGian);

        if (txt != null) txt.gameObject.SetActive(false);
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    private void Update()
    {
        CapNhatHienThiXu();
        CapNhatFeed();
        GuiGocCamera();

        if (danhSachDongXu != null)
        {
            float goc = tocDoXoayDongXu * Time.deltaTime;
            for (int i = 0; i < danhSachDongXu.Length; i++)
            {
                GameObject xu = danhSachDongXu[i];
                if (xu != null && xu.activeInHierarchy)
                    xu.transform.Rotate(0f, goc, 0f, Space.World);
            }
        }

        // ESC chỉ có tác dụng khi đã bắt đầu đua
        if (Input.GetKeyDown(KeyCode.Escape) && daBatDauDua && !ketThucGame && !winGame && !dangChoKetQua)
        {
            if (daBiLoai)
            {
                // Đang xem trận: ESC mở/đóng bảng "Ở lại xem / Thoát"
                if (dangXemTran && eliminatePanelObject != null)
                {
                    bool bat = !eliminatePanelObject.activeSelf;
                    eliminatePanelObject.SetActive(bat);
                    if (bat) eliminatePanelObject.transform.SetAsLastSibling();
                }
            }
            else if (isPaused) ResumeGame();
            else PauseGame();
        }

        bool dangDua = daBatDauDua && !daBiLoai && !ketThucGame && !winGame;

        // Solo: pause thì đồng hồ dừng. Nhiều người: trận vẫn chạy
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

        if (dangDua)
        {
            XuLyVaChamChoXe xe = LayXeCuaMinh();
            if (xe != null && xe.transform.position.y <= mucNuocY)
                ChamNuocTruThoiGian();
        }
    }

    // =========================================================================
    // PAUSE / RESUME / THOÁT
    // =========================================================================
    public void PauseGame()
    {
        if (!daBatDauDua || ketThucGame || winGame) return;

        isPaused = true;
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
    }

    // Nút "Ở LẠI XEM" trên EliminatePanel: đóng bảng, tiếp tục xem
    public void ChonOLaiXem()
    {
        XoaTieuDiemUI();
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        BatDauXemTran();
    }

    // Nút "THOÁT" trên EliminatePanel / ResultPanel
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
            // Không bật bảng "Ở lại xem / Thoát" nữa cho đỡ che màn hình.
            // Vào xem luôn; bấm ESC nếu muốn mở bảng thoát.
            HienThongBaoSuKien("BẠN ĐÃ BỊ LOẠI", Color.yellow, 2f);

            BatDauXemTran();

            // Dự phòng: không vào xem được (không có camera) thì mới hiện bảng để còn thoát
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

    // doCoNguoiRoiPhong = true: có người thoát phòng -> KHÔNG tự cho người còn lại thắng
    private void KiemTraNguoiSotLaiCuoiCung(bool doCoNguoiRoiPhong = false)
    {
        // Chỉ MasterClient được quyền xác định người sống sót cuối cùng.
        // Tránh mỗi máy tự đếm theo trạng thái đồng bộ khác nhau và chọn nhầm người thắng.
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
            // Người khác thoát phòng làm chỉ còn 1 người đang đua: để họ đua tiếp
            // tới đích hoặc hết giờ, không tự nhiên hiện chiến thắng.
            if (doCoNguoiRoiPhong) return;

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
        KiemTraNguoiSotLaiCuoiCung();
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

        HienThongBaoSuKien($"+{Mathf.FloorToInt(thoiGianHoiKhiQuaCheckPoint)} GIÂY! (ĐÃ QUA CHECKPOINT)", Color.white, 2f);
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
        if (!daBatDauDua || daBiLoai || ketThucGame || winGame) return;

        if (thoiDiemBatDauDua > 0f && Time.time - thoiDiemBatDauDua < 1.5f) return;

        if (congDichCache == null)
            congDichCache = GameObject.FindGameObjectWithTag("WinPoint");

        XuLyVaChamChoXe xe = LayXeCuaMinh();

        // Chặn thắng giả: xe còn quá xa vạch đích
        if (congDichCache != null && xe != null)
        {
            float kc = Vector3.Distance(xe.transform.position, congDichCache.transform.position);
            if (kc > 25f)
            {
                Debug.LogWarning($"[CHẶN THẮNG GIẢ] Xe còn cách đích {kc:F1}m");
                return;
            }
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

        // Không công nhận một yêu cầu thắng từ người đã rời phòng hoặc đã bị loại.
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

        // Nhiều người + đã biết người thắng: cho xem người thắng vài giây rồi mới hiện bảng
        if (!daXemXong && cheDoNhieuNguoi && actorNguoiThang > 0)
        {
            if (!daBiLoai) PhatAmThanhThua();
            daBiLoai = true; // dừng đồng hồ, chặn xử lý loại lần nữa
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

        if (nhacNenSource != null) nhacNenSource.Stop();
        if (!daBiLoai) PhatAmThanhThua();

        int tongXu = PlayerInventory.GetCoins();
        string noiDungKetQua = $"{lyDoThua}\nTHƯỞNG: 0 XU | TỔNG XU: {tongXu}";

        HienBangKetQua(false, noiDungKetQua, 0, tongXu, Color.red);
    }

    // Nhiều người: chỉ hiện ResultPanel. Solo: chỉ hiện bảng Thắng/Thua cũ.
    // Chỉ khi thiếu bảng mới hiện chữ lớn giữa màn hình (tránh hiện trùng 2 nơi).
    private void HienBangKetQua(bool thang, string noiDung, int xuNhanDuoc, int tongXu, Color mauChuDuPhong)
    {
        if (timeGameObject != null) timeGameObject.SetActive(false);
        if (eliminatePanelObject != null) eliminatePanelObject.SetActive(false);
        if (feedText != null) feedText.text = "";
        feed.Clear();

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
            if (bang != null) bang.SetActive(true);
        }

        if (!coBang) HienThongBaoSuKien(noiDung, mauChuDuPhong, 4f);
    }

    private void MoResultPanel(string noiDungKetQua, bool laNguoiThang, int xuNhanDuoc, int tongXu)
    {
        if (resultPanelObject == null)
        {
            Debug.LogWarning("[GameManager] Chưa gán resultPanelObject cho chế độ multiplayer!");
            return;
        }

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