using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class scooter : MonoBehaviour
{
    public static readonly List<scooter> TatCaXe = new List<scooter>();

    [System.Serializable]
    public class MauXe
    {
        public string ten = "Mẫu xe";
        [Tooltip("MỌI object phải BẬT khi chọn mẫu này: thân xe, bánh xe, phụ kiện. Mẫu nào dùng chung bánh thì kéo cùng bánh vào cả hai.")]
        public GameObject[] cacManh;
    }

    [Header("Di chuyển")]
    [SerializeField] private float tocDoXe = 15f;
    [SerializeField] private float heSoTangToc = 1.6f;
    [SerializeField] private float lucReXe = 140f;    // cũ 85: tốc độ xoay (độ/giây)
    [SerializeField] private float giaToc = 12f;      // cũ 6: tăng/giảm tốc nhanh hơn
    [SerializeField] private float doMuotLai = 20f;   // cũ 10: đánh lái vào nhanh hơn

    [Header("Phanh & Lùi")]
    [SerializeField] private GameObject hieuUngPhanh;

    [Header("Nhảy")]
    [SerializeField] private float lucNhay = 8f;
    [SerializeField] private float thoiGianCoyote = 0.15f;
    [SerializeField] private float thoiGianDemNhay = 0.15f;
    [SerializeField, Tooltip("Hệ số trọng lực lúc đang rơi (>1: rơi nhanh hơn, cú nhảy gọn hơn, bớt lơ lửng)")]
    private float heSoRoiNhanh = 1.5f;
    [SerializeField, Tooltip("Chân xe cao hơn đỉnh vật cản chừng này thì coi như đã nhảy qua, không bị chặn (m)")]
    private float doCaoVuotVatCan = 0.05f;

    [Header("Bám đường (tia dò)")]
    [SerializeField] private float maxDepenetration = 2f;
    [SerializeField] private float doCaoTiaRay = 0.7f;
    [SerializeField] private float khoangBamDat = 0.9f;
    [SerializeField] private float doMuotPhapTuyen = 0.35f;
    [SerializeField, Tooltip("Độ cứng giữ độ cao so với mặt đường")]
    private float heSoHutDat = 12f;
    [SerializeField, Tooltip("Tốc độ hạ xe tối đa để theo mặt đường (m/s)")]
    private float toiDaHutDat = 4f;
    [SerializeField, Tooltip("Tốc độ nâng xe tối đa khi hiệu chỉnh độ cao (m/s), nhỏ hơn để hết nảy")]
    private float toiDaNangLen = 1.5f;
    [SerializeField, Tooltip("Tia dò thấy mặt đường lệch quá mức này thì bỏ qua")]
    private float doLechToiDaTiaDo = 0.6f;
    [SerializeField, Tooltip("Tia dò hụt ngắn hơn mức này (giây) thì vẫn giữ trạng thái bám đất (khe nối đường)")]
    private float thoiGianGiuBam = 0.12f;

    [Header("Chống bay lên")]
    [SerializeField, Tooltip("Khi không nhảy mà vận tốc bay lên vượt mức này thì bị cắt lại (m/s)")]
    private float vanTocLenToiDa = 3f;

    [Header("Mẫu xe (thứ tự = thứ tự trong Shop)")]
    [SerializeField, Tooltip("Để trống = tự dò từ cây prefab. Mẫu nào hiện thiếu bánh/thân thì khai báo ở đây: mỗi phần tử liệt kê MỌI object cần bật cho mẫu đó.")]
    private MauXe[] cacMauXe;
    [SerializeField, Tooltip("(Cũ) Dùng khi 'Cac Mau Xe' để trống")]
    private Transform[] danhSachMauXe;
#pragma warning disable CS0414 // Retained because existing prefabs serialize this legacy setting.
    [SerializeField, Tooltip("Bật mọi mesh thuộc mẫu đang chọn (phòng khi prefab để ẩn một số phần)")]
    private bool batMoiMeshCuaMau = true;
#pragma warning restore CS0414

    [Header("Chạy mượt")]
    [SerializeField] private bool taoMaterialKhongMaSat = true;
    [SerializeField] private bool boMeshColliderTrenXe = true;
    [SerializeField] private bool boQuaVaChamMatDuong = false;
    [SerializeField] private float doCaoGoBoQua = 0.12f;

    [Tooltip("Xe xuyên qua vật có tên chứa các từ này, cách nhau bằng dấu phẩy.")]
    [SerializeField] private string tuKhoaBoQuaVaCham = "tree,grass,prop,decor";

    [Header("Vật cản cứng")]
    [SerializeField, Tooltip("Bật: vật cản là vật CỨNG, xe đâm vào bị chặn. Tắt: xe xuyên qua như cũ.")]
    private bool vatCanLaVatCung = true;
    [SerializeField, Tooltip("Tên (hoặc tên object cha) chứa các từ này = vật cứng. Cách nhau bằng dấu phẩy.")]
    private string tuKhoaVatCung = "obstacle,barricade,cone,guardrail,fence,barrier";
    [SerializeField, Tooltip("Tên chứa các từ này thì KHÔNG BAO GIỜ là vật cứng (checkpoint, đích, cổng...).")]
    private string tuKhoaKhongCung = "checkpoint,finish,goal,gate,trigger,start";
    [SerializeField, Tooltip("Quét trước mặt xe để chặn vật cản cứng, chống xuyên khi chạy nhanh")]
    private bool quetChanTruoc = true;
    [SerializeField] private float banKinhQuet = 0.35f;
    [SerializeField, Tooltip("Độ cao tia quét so với chân xe")]
    private float doCaoQuet = 0.3f;

    [Header("Chặn mép đường (tường vô hình)")]
    [SerializeField, Tooltip("Bật: xe sắp chạy ra khỏi mặt đường (chỗ không có đường bên dưới) thì bị chặn như có lan can. Tắt nếu map có đoạn nhảy qua vực.")]
    private bool chanMepDuong = true;
    [SerializeField, Tooltip("Khoảng dò phía trước xe (m), cộng thêm theo tốc độ")]
    private float khoangDoMepTruoc = 0.8f;
    [SerializeField, Tooltip("Khoảng dò sang hai bên xe (m)")]
    private float khoangDoMepNgang = 0.6f;

    [Header("Hồi sinh khi rơi nước")]
    [SerializeField] private float nhacLenKhiHoiSinh = 0.5f;
    [SerializeField] private float thoiGianChoHoiSinh = 1f;
    [SerializeField] private float docaoRoiNuoc = -0.3f;
    [SerializeField] private float bienChamNuoc = 0.5f;

    [Header("Mô hình chạm đường")]
    [SerializeField] private bool tuHaMoHinhChamDat = false;
    [SerializeField] private float moHinhHaThem = 0f;

    [Header("Chẩn đoán (TẮT khi chơi: ghi log trong lúc lái gây giật)")]
    [SerializeField] private bool ghiLogChanDoan = false;

    private const float NGUONG_BI_CHAN = 5f;
    private const float THOI_GIAN_BI_CHAN = 0.3f;
    private const int SO_COLLIDER_MOI_DOT = 150;

    private bool xeDangBat = false;
    private bool dangLui = false;
    private bool dangTangToc = false;
    private float dauVaoRe;
    private float gocReHienTai = 0f;
    private float tocDoDangChay = 0f;
    private float thoiGianBiChan = 0f;

    private float thoiDiemChamDat = -10f;
    private float thoiDiemBamNhay = -10f;
    private float thoiDiemNhayCuoi = -10f;
    private float thoiDiemThayMat = -10f;

    private Vector3 phapTuyenDat = Vector3.up;
    private bool dangBamDat = false;
    private readonly RaycastHit[] bufferRay = new RaycastHit[16];
    private readonly RaycastHit[] bufferMep = new RaycastHit[8];
    private float chieuCaoPivot = 0f;
    private float suaCaoDo = 0f;

    private readonly Vector3[] phapTuyenTuong = new Vector3[8];
    private int soTuong = 0;
    private readonly Vector3[] phapTuyenDoc = new Vector3[4];
    private int soDoc = 0;
    private bool chamSanVat = false;

    private Vector3 viTriXuatPhat;
    private Quaternion huongXuatPhat;
    private float thoiDiemHoiSinh = -10f;
    private bool daLuuLucBatDau = false;
    private int soLanCanMoHinh = 0;
    private float thoiDiemLogChan = -10f;
    private float thoiDiemLogMat = -10f;
    private bool daBamDatTruoc = false;
    private bool remoteControlled;
    private bool networkControlled;

    private string[] tuKhoaVat = new string[0];
    private string[] tuKhoaCung = new string[0];
    private string[] tuKhoaLoaiTru = new string[0];
    private readonly Dictionary<Collider, bool> cacheMatDuong = new Dictionary<Collider, bool>();
    private readonly Dictionary<Collider, bool> cacheVatCung = new Dictionary<Collider, bool>();
    private readonly Dictionary<Collider, bool> cacheNuoc = new Dictionary<Collider, bool>();
    private readonly HashSet<Collider> daBoQuaCollider = new HashSet<Collider>();

    private List<GameObject[]> mauXeRuntime;
    private int mauDangDung = -1;

    private Rigidbody rb;
    private Collider[] tatCaColliderXe;
    private MeshRenderer mrIcon;
    private Mesh meshMuiTen;
    private Material matMuiTen;

    private GameNetworkManager gnmCache;
    private GameManager gmCache;
    private float thoiDiemTimGM = 0f;

    private static PhysicsMaterial matKhongMaSat;
    private static int cachedLayerMinimap = -2;

    // Cache ô đang chọn trong EventSystem: không gọi GetComponent mỗi frame
    private static GameObject selCache;
    private static InputField selInput;
    private static TMP_InputField selTmp;

    private bool LaXeCuaMinh
    {
        get { return !remoteControlled; }
    }

    public float DoNghieng { get { return gocReHienTai; } }
    public float TocDoToiDa { get { return tocDoXe; } }
    public bool DangLui { get { return dangLui; } }
    public bool IsRemoteControlled { get { return remoteControlled; } }
    public float NetworkThrottle { get { return dangTangToc ? 1f : dangLui ? -1f : 0f; } }
    public float NetworkSteer { get { return dauVaoRe; } }
    public int SoMauXe { get { return 2; } }
    public int MauDangDung { get { return mauDangDung; } }

    public void SetNetworkControlled(bool value)
    {
        networkControlled = value;
        if (value && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void SetRemoteControlled(bool value)
    {
        remoteControlled = value;
        if (value && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private static int LayLayerMinimap()
    {
        if (cachedLayerMinimap == -2)
            cachedLayerMinimap = LayerMask.NameToLayer("Minimap");
        return cachedLayerMinimap;
    }

    private GameManager LayGM()
    {
        if (gmCache == null && Time.unscaledTime >= thoiDiemTimGM)
        {
            thoiDiemTimGM = Time.unscaledTime + 1f;
            gmCache = GameManager.Instance;
        }
        return gmCache;
    }

    private void OnEnable()
    {
        if (!TatCaXe.Contains(this)) TatCaXe.Add(this);
    }

    private void OnDisable()
    {
        TatCaXe.Remove(this);
    }

    // =========================================================================
    // MẪU XE
    // =========================================================================
    // Mặc định bỏ qua nếu xe đã đúng mẫu này (nhiều script cùng gọi, chỉ làm 1 lần).
    // ep = true để dựng lại (sau khi nâng xe lên mặt đường...).
    public void ApDungMauXe(int indexXe, bool ep = false)
    {
        if (indexXe < 0) indexXe = 0;
        if (!ep && indexXe == mauDangDung) return;

        mauDangDung = indexXe;

        if (gnmCache == null) gnmCache = FindFirstObjectByType<GameNetworkManager>();
        float kichThuoc = (gnmCache != null) ? gnmCache.kichThuocXeChuan : 1.6f;

        GameNetworkManager.KichHoatDungMauXe(gameObject, indexXe, kichThuoc);
    }

    // =========================================================================
    // KHỞI TẠO
    // =========================================================================
    private static string[] TachTuKhoa(string chuoi)
    {
        var ds = new List<string>();
        if (string.IsNullOrWhiteSpace(chuoi)) return ds.ToArray();

        foreach (string k in chuoi.ToLower().Split(','))
        {
            string s = k.Trim();
            if (s.Length > 0) ds.Add(s);
        }
        return ds.ToArray();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        DonColliderXe();

        tuKhoaCung = vatCanLaVatCung ? TachTuKhoa(tuKhoaVatCung) : new string[0];
        tuKhoaLoaiTru = TachTuKhoa(tuKhoaKhongCung);

        var dsXuyen = new List<string>();
        foreach (string s in TachTuKhoa(tuKhoaBoQuaVaCham))
        {
            if (vatCanLaVatCung && System.Array.IndexOf(tuKhoaCung, s) >= 0) continue;
            dsXuyen.Add(s);
        }
        tuKhoaVat = dsXuyen.ToArray();

        if (hieuUngPhanh != null) hieuUngPhanh.SetActive(false);

        TaoMuiTenMinimap();
        DamBaoCameraThayXe();

        if (tuHaMoHinhChamDat)
            Invoke(nameof(CanMoHinhChamDat), 1.5f);

        if (LaXeCuaMinh)
        {
            int indexTrangBi = VehicleCatalog.GetSelectedIndex();
            ApDungMauXe(indexTrangBi);
            LuuDiemXuatPhat();

            rb.isKinematic = networkControlled;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxDepenetrationVelocity = maxDepenetration;

            TinhChieuCaoPivot();
            ThietLapColliderXe();

            if (ghiLogChanDoan)
                Debug.Log($"[XE] scooter v6 | vatCung={vatCanLaVatCung} quet={quetChanTruoc} mepDuong={chanMepDuong} chieuCaoPivot={chieuCaoPivot:F2} tuKhoaCung=[{tuKhoaVatCung}] tuKhoaXuyen=[{tuKhoaBoQuaVaCham}]");

            // Quét toàn bộ collider của scene rất nặng: chia nhỏ ra nhiều frame thay vì dồn vào 1 frame
            StartCoroutine(BoQuaVatCanNhoTheoDot());
            if (boQuaVaChamMatDuong) XuyenMatDuong();

            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                Transform diemBamCam = transform.Find("pointFollowTarget");
                cam.Target.TrackingTarget = diemBamCam != null ? diemBamCam : transform;
            }

            MinimapController minimap = FindFirstObjectByType<MinimapController>();
            if (minimap != null) minimap.targetXeCuaMinh = transform;
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void OnDestroy()
    {
        TatCaXe.Remove(this);
        if (matMuiTen != null) Destroy(matMuiTen);
        if (meshMuiTen != null) Destroy(meshMuiTen);
    }

    private static bool ColliderDungDuoc(Collider c)
    {
        return c != null && c.enabled && c.gameObject.activeInHierarchy;
    }

    private void DonColliderXe()
    {
        bool coColliderGoc = false;
        foreach (Collider c in GetComponents<Collider>())
        {
            if (c != null && c.enabled && !c.isTrigger) { coColliderGoc = true; break; }
        }

        bool xoaMesh = boMeshColliderTrenXe && coColliderGoc;
        var ds = new List<Collider>();

        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            if (xoaMesh && c is MeshCollider && c.gameObject != gameObject && !c.isTrigger)
            {
                Destroy(c);
                continue;
            }
            ds.Add(c);
        }

        tatCaColliderXe = ds.ToArray();
    }

    private void TinhChieuCaoPivot()
    {
        float thap = float.MaxValue;
        foreach (Collider c in tatCaColliderXe)
        {
            if (!ColliderDungDuoc(c) || c.isTrigger) continue;
            thap = Mathf.Min(thap, c.bounds.min.y);
        }

        chieuCaoPivot = thap == float.MaxValue ? 0f : Mathf.Clamp(transform.position.y - thap, 0f, 1.5f);
    }

    private void DamBaoCameraThayXe()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int layerMinimap = LayLayerMinimap();
        int canBat = 0;

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            int layer = r.gameObject.layer;
            if (layer == layerMinimap) continue;
            canBat |= 1 << layer;
        }

        int thieu = canBat & ~cam.cullingMask;
        if (thieu != 0) cam.cullingMask |= thieu;

        if (cam.GetComponent<AudioListener>() != null)
        {
            foreach (AudioListener al in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (al.gameObject != cam.gameObject && al.enabled)
                    al.enabled = false;
            }
        }
    }

    private void CanMoHinhChamDat()
    {
        bool sanSang = DoMatDuong(out _, out float matY) && (!LaXeCuaMinh || dangBamDat);

        if (!sanSang)
        {
            if (++soLanCanMoHinh < 4)
                Invoke(nameof(CanMoHinhChamDat), 1f);
            return;
        }

        float duoi = float.MaxValue;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(false))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (r.gameObject.name.Trim() == "MinimapIcon") continue;
            duoi = Mathf.Min(duoi, r.bounds.min.y);
        }

        if (duoi == float.MaxValue) return;

        float hoXuong = (duoi - matY) + moHinhHaThem;
        if (Mathf.Abs(hoXuong) < 0.05f || hoXuong > 1.5f || hoXuong < -1f) return;

        foreach (Transform con in transform)
        {
            string ten = con.name.Trim();
            if (ten == "MinimapIcon" || ten == "pointFollowTarget") continue;
            if (con.GetComponentInChildren<Camera>(true) != null) continue;
            if (con.GetComponentInChildren<Collider>(true) != null) continue;
            if (con.GetComponentInChildren<MeshRenderer>(true) == null &&
                con.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                continue;

            con.position += Vector3.down * hoXuong;
        }
    }

    public void LuuDiemXuatPhat()
    {
        viTriXuatPhat = transform.position;
        huongXuatPhat = transform.rotation;
    }

    private void HoiSinh()
    {
        if (Time.time - thoiDiemHoiSinh < thoiGianChoHoiSinh) return;
        thoiDiemHoiSinh = Time.time;
        // Bị đưa về vạch xuất phát (rơi nước / ra khỏi đường) thì trừ thời gian giống như chạm nước
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.ChamNuoc();

        if (ghiLogChanDoan)
            Debug.LogWarning($"[XE HỒI SINH] tại {transform.position}, bamDat={dangBamDat}, tốc độ={tocDoDangChay:F1}");

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        tocDoDangChay = 0f;
        gocReHienTai = 0f;
        thoiGianBiChan = 0f;
        soTuong = 0;
        soDoc = 0;
        chamSanVat = false;
        xeDangBat = false;
        dangBamDat = false;
        phapTuyenDat = Vector3.up;
        thoiDiemChamDat = -10f;
        thoiDiemBamNhay = -10f;
        thoiDiemThayMat = -10f;

        Vector3 pos = viTriXuatPhat + Vector3.up * nhacLenKhiHoiSinh;
        rb.position = pos;
        rb.rotation = huongXuatPhat;
        transform.SetPositionAndRotation(pos, huongXuatPhat);
        Physics.SyncTransforms();
    }

    private void TaoMuiTenMinimap()
    {
        foreach (Transform con in GetComponentsInChildren<Transform>(true))
        {
            if (con != transform && con.name.Trim() == "MinimapIcon")
                Destroy(con.gameObject);
        }

        GameObject muiTenObj = new GameObject("MinimapIcon");
        muiTenObj.transform.SetParent(transform);
        muiTenObj.transform.SetLocalPositionAndRotation(new Vector3(0f, 3.5f, 0f), Quaternion.identity);
        muiTenObj.transform.localScale = new Vector3(4.5f, 1f, 4.5f);

        int layerMinimap = LayLayerMinimap();
        if (layerMinimap != -1) muiTenObj.layer = layerMinimap;

        MeshFilter mf = muiTenObj.AddComponent<MeshFilter>();
        mrIcon = muiTenObj.AddComponent<MeshRenderer>();

        meshMuiTen = new Mesh();
        meshMuiTen.vertices = new Vector3[]
        {
            new Vector3( 1.2f, 0f,  0.0f),
            new Vector3(-0.9f, 0f,  0.85f),
            new Vector3(-0.4f, 0f,  0.0f),
            new Vector3(-0.9f, 0f, -0.85f)
        };
        meshMuiTen.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        meshMuiTen.RecalculateNormals();
        mf.sharedMesh = meshMuiTen;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");

        matMuiTen = new Material(unlit);
        mrIcon.sharedMaterial = matMuiTen;

        CapNhatMauMuiTen();
        Invoke(nameof(CapNhatMauMuiTen), 0.25f);
    }

    private void CapNhatMauMuiTen()
    {
        if (matMuiTen == null) return;
        Color mau = LaXeCuaMinh ? Color.green : Color.red;
        matMuiTen.SetColor("_BaseColor", mau);
        matMuiTen.color = mau;
    }

    // =========================================================================
    // NƯỚC / VA CHẠM
    // =========================================================================
    // gameObject.name tạo chuỗi mới mỗi lần đọc: cache theo collider để không sinh rác trong OnCollisionStay / OnTriggerStay
    private bool LaNuoc(Collider c)
    {
        if (c == null) return false;
        if (cacheNuoc.TryGetValue(c, out bool kq)) return kq;

        string ten = c.gameObject.name;
        kq = ten.Contains("Sea_Plane") || ten.Contains("Water");
        cacheNuoc[c] = kq;
        return kq;
    }

    private bool XeDaXuongNuoc()
    {
        return !dangBamDat && transform.position.y < docaoRoiNuoc + bienChamNuoc;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!LaXeCuaMinh) return;
        if (LaNuoc(other) && XeDaXuongNuoc()) HoiSinh();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!LaXeCuaMinh) return;
        if (LaNuoc(other) && XeDaXuongNuoc()) HoiSinh();
    }

    // Xe người khác không được đẩy / hất xe mình: bỏ va chạm giữa các xe (mỗi collider chỉ xử lý 1 lần)
    private void BoQuaXeKhac(Collision c)
    {
        Rigidbody orb = c.rigidbody;
        if (orb == null || orb == rb) return;

        Collider oc = c.collider;
        if (oc == null || daBoQuaCollider.Contains(oc)) return;

        if (orb.TryGetComponent(out scooter khac) && khac != this)
        {
            daBoQuaCollider.Add(oc);
            IgnoreVoiXe(oc);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        BoQuaXeKhac(collision);

        if (LaNuoc(collision.collider) && XeDaXuongNuoc())
        {
            HoiSinh();
            return;
        }

        GhiNhanTuong(collision);
        GhiLogNeuBiChan(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        BoQuaXeKhac(collision);

        GhiNhanTuong(collision);
        GhiLogNeuBiChan(collision);
    }

    private bool LaVatCanCung(Collider c)
    {
        if (!vatCanLaVatCung || c == null || tuKhoaCung.Length == 0) return false;
        if (c.isTrigger) return false;
        if (cacheVatCung.TryGetValue(c, out bool kq)) return kq;

        bool cung = false;
        for (Transform t = c.transform; t != null && !cung; t = t.parent)
        {
            string ten = t.name.ToLower();

            bool loaiTru = false;
            for (int i = 0; i < tuKhoaLoaiTru.Length; i++)
            {
                if (ten.Contains(tuKhoaLoaiTru[i])) { loaiTru = true; break; }
            }
            if (loaiTru) break;

            for (int i = 0; i < tuKhoaCung.Length; i++)
            {
                if (ten.Contains(tuKhoaCung[i])) { cung = true; break; }
            }
        }

        cacheVatCung[c] = cung;
        return cung;
    }

    private bool LaGoThap(ContactPoint cp)
    {
        if (LaVatCanCung(cp.otherCollider)) return false;

        float duoiXe = rb.position.y - chieuCaoPivot;
        return cp.point.y < duoiXe + doCaoGoBoQua;
    }

    private void ThemPhapTuyenTuong(Vector3 n)
    {
        n.y = 0f;
        if (n.sqrMagnitude < 0.01f) return;
        n.Normalize();

        // Bỏ pháp tuyến trùng hướng: đỡ lặp trong TruVaoTuong
        for (int i = 0; i < soTuong; i++)
            if (Vector3.Dot(phapTuyenTuong[i], n) > 0.98f) return;

        if (soTuong < phapTuyenTuong.Length)
            phapTuyenTuong[soTuong++] = n;
    }

    private void ThemPhapTuyenDoc(Vector3 n)
    {
        n.Normalize();

        for (int i = 0; i < soDoc; i++)
            if (Vector3.Dot(phapTuyenDoc[i], n) > 0.98f) return;

        if (soDoc < phapTuyenDoc.Length)
            phapTuyenDoc[soDoc++] = n;
    }

    private void GhiNhanTuong(Collision c)
    {
        if (LaNuoc(c.collider)) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            ContactPoint cp = c.GetContact(i);

            if (cp.normal.y >= 0.7f) { chamSanVat = true; continue; }
            if (cp.normal.y > 0.25f) { ThemPhapTuyenDoc(cp.normal); continue; }
            if (LaGoThap(cp)) continue;
            if (XeNamTrenVat(cp.otherCollider)) continue;

            ThemPhapTuyenTuong(cp.normal);
        }
    }

    // Chân xe đã cao hơn đỉnh vật cản => đang nhảy qua nó: không coi là tường, không quét chặn
    private bool XeNamTrenVat(Collider c)
    {
        if (c == null) return false;

        float chanXe = rb.position.y - chieuCaoPivot;
        return c.bounds.max.y <= chanXe + doCaoVuotVatCan;
    }

    private Vector3 TruVaoTuong(Vector3 v)
    {
        for (int i = 0; i < soTuong; i++)
        {
            Vector3 n = phapTuyenTuong[i];
            float d = Vector3.Dot(v, n);
            if (d < 0f) v -= n * d;
        }
        return v;
    }

    private static string DuongDan(Transform t)
    {
        string s = t.name;
        int dem = 0;
        while (t.parent != null && dem++ < 4)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }

    private void GhiLogNeuBiChan(Collision c)
    {
        if (!ghiLogChanDoan) return;
        if (Time.time - thoiDiemLogChan < 0.5f) return;
        if (tocDoDangChay < 5f || LaNuoc(c.collider)) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            ContactPoint cp = c.GetContact(i);
            if (cp.normal.y < 0.5f && !LaGoThap(cp))
            {
                thoiDiemLogChan = Time.time;
                Debug.LogWarning($"[XE BỊ CHẶN] bởi '{DuongDan(c.collider.transform)}' ({c.collider.GetType().Name}), tốc độ {tocDoDangChay:F1}", c.collider);
                return;
            }
        }
    }

    private void ThietLapColliderXe()
    {
        if (!taoMaterialKhongMaSat) return;

        if (matKhongMaSat == null)
        {
            matKhongMaSat = new PhysicsMaterial("ScooterRuntime")
            {
                bounciness = 0f,
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        foreach (Collider c in tatCaColliderXe)
        {
            if (c != null && !c.isTrigger)
                c.sharedMaterial = matKhongMaSat;
        }
    }

    private static bool KhopDauTu(string ten, string tuKhoa)
    {
        if (string.IsNullOrEmpty(tuKhoa)) return false;

        int i = ten.IndexOf(tuKhoa, System.StringComparison.Ordinal);
        while (i >= 0)
        {
            if (i == 0 || !char.IsLetter(ten[i - 1])) return true;
            i = ten.IndexOf(tuKhoa, i + 1, System.StringComparison.Ordinal);
        }
        return false;
    }

    private void IgnoreVoiXe(Collider vat)
    {
        foreach (Collider c in tatCaColliderXe)
        {
            if (ColliderDungDuoc(c)) Physics.IgnoreCollision(c, vat, true);
        }
    }

    // Quét từng đợt SO_COLLIDER_MOI_DOT collider mỗi frame (trước đây quét hết trong 1 frame gây khựng lúc vào trận)
    private IEnumerator BoQuaVatCanNhoTheoDot()
    {
        if (tuKhoaVat.Length == 0) yield break;

        Collider[] tatCa = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int dem = 0;

        for (int i = 0; i < tatCa.Length; i++)
        {
            XuLyBoQuaVatCanNho(tatCa[i]);

            if (++dem >= SO_COLLIDER_MOI_DOT)
            {
                dem = 0;
                yield return null;
            }
        }
    }

    private void XuLyBoQuaVatCanNho(Collider vat)
    {
        if (!ColliderDungDuoc(vat) || vat.isTrigger) return;
        if (vat.transform.IsChildOf(transform)) return;
        if (vat.GetComponentInParent<Rigidbody>() != null) return;
        if (LaVatCanCung(vat)) return;

        string tenRieng = vat.gameObject.name.ToLower();
        if (tenRieng.Contains("road") || tenRieng.Contains("sea") || tenRieng.Contains("water"))
            return;

        bool khop = false;
        for (Transform t = vat.transform; t != null && !khop; t = t.parent)
        {
            string ten = t.name.ToLower();
            if (ten.Contains("road")) break;

            foreach (string k in tuKhoaVat)
            {
                if (KhopDauTu(ten, k)) { khop = true; break; }
            }
        }

        if (khop) IgnoreVoiXe(vat);
    }

    private void XuyenMatDuong()
    {
        foreach (Collider vat in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!ColliderDungDuoc(vat) || vat.isTrigger) continue;
            if (vat.transform.IsChildOf(transform)) continue;
            if (vat.attachedRigidbody != null) continue;
            if (LaVatCanCung(vat)) continue;

            string ten = vat.gameObject.name.ToLower();
            if (ten.Contains("road_") || ten.Contains("platform") || ten.Contains("ramp"))
                IgnoreVoiXe(vat);
        }
    }

    private bool LaMatDuong(Collider c)
    {
        if (c == null) return false;
        if (cacheMatDuong.TryGetValue(c, out bool kq)) return kq;

        bool ok = true;

        if (LaVatCanCung(c))
        {
            ok = false;
        }
        else
        {
            for (Transform t = c.transform; t != null && ok; t = t.parent)
            {
                string ten = t.name.ToLower();
                if (ten.Contains("sea_plane") || ten.Contains("water")) { ok = false; break; }
                if (ten.Contains("road")) { ok = true; break; }

                foreach (string k in tuKhoaVat)
                {
                    if (KhopDauTu(ten, k)) { ok = false; break; }
                }
            }
        }

        cacheMatDuong[c] = ok;
        return ok;
    }

    private bool DoMatDuong(out Vector3 phapTuyen, out float matY)
    {
        phapTuyen = Vector3.up;
        matY = 0f;
        if (rb == null) return false;

        Vector3 goc = rb.position + Vector3.up * doCaoTiaRay;

        int n = Physics.RaycastNonAlloc(
            goc, Vector3.down, bufferRay,
            doCaoTiaRay + khoangBamDat, ~0,
            QueryTriggerInteraction.Ignore);

        float gan = float.MaxValue;
        bool co = false;

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = bufferRay[i];
            Collider c = h.collider;

            if (c == null || c.attachedRigidbody != null) continue;
            if (h.normal.y < 0.7f) continue;
            if (!LaMatDuong(c)) continue;

            if (h.distance < gan)
            {
                gan = h.distance;
                phapTuyen = h.normal;
                matY = h.point.y;
                co = true;
            }
        }

        return co;
    }

    private void QuetVatCanCungPhiaTruoc(Vector3 huongTien, float dt)
    {
        if (!quetChanTruoc || !vatCanLaVatCung) return;
        if (Mathf.Abs(tocDoDangChay) < 0.5f) return;

        Vector3 huong = huongTien * Mathf.Sign(tocDoDangChay);
        huong.y = 0f;
        if (huong.sqrMagnitude < 0.0001f) return;
        huong.Normalize();

        float quang = Mathf.Abs(tocDoDangChay) * dt + 0.15f;
        Vector3 goc = rb.position + Vector3.up * doCaoQuet;

        int n = Physics.SphereCastNonAlloc(
            goc, banKinhQuet, huong, bufferRay, quang, ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = bufferRay[i];
            Collider c = h.collider;

            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (!LaVatCanCung(c)) continue;
            if (XeNamTrenVat(c)) continue;

            Vector3 phap = h.normal;
            if (phap.sqrMagnitude < 0.0001f) phap = -huong;

            ThemPhapTuyenTuong(phap);

            Vector3 pn = phap; pn.y = 0f;
            if (pn.sqrMagnitude > 0.01f && Vector3.Dot(huong, -pn.normalized) > 0.9f)
                tocDoDangChay = 0f;
        }
    }

    private bool CoMatDuongTai(Vector3 diem)
    {
        Vector3 goc = diem + Vector3.up * doCaoTiaRay;

        int n = Physics.RaycastNonAlloc(
            goc, Vector3.down, bufferMep,
            doCaoTiaRay + khoangBamDat + 0.6f, ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            Collider c = bufferMep[i].collider;
            if (c == null || c.attachedRigidbody != null) continue;
            if (bufferMep[i].normal.y < 0.7f) continue;
            if (LaMatDuong(c)) return true;
        }
        return false;
    }

    private void ChanMepDuong(Vector3 huongTien)
    {
        if (!chanMepDuong || !dangBamDat) return;
        if (Mathf.Abs(tocDoDangChay) < 0.5f) return;

        Vector3 huong = huongTien * Mathf.Sign(tocDoDangChay);
        huong.y = 0f;
        if (huong.sqrMagnitude < 0.0001f) return;
        huong.Normalize();

        Vector3 ngang = Vector3.Cross(Vector3.up, huong);
        Vector3 vt = rb.position;
        float nhin = khoangDoMepTruoc + Mathf.Abs(tocDoDangChay) * Time.fixedDeltaTime * 2f;

        if (!CoMatDuongTai(vt + huong * nhin)) ThemPhapTuyenTuong(-huong);

        Vector3 giua = vt + huong * 0.3f;
        if (!CoMatDuongTai(giua + ngang * khoangDoMepNgang)) ThemPhapTuyenTuong(-ngang);
        if (!CoMatDuongTai(giua - ngang * khoangDoMepNgang)) ThemPhapTuyenTuong(ngang);
    }

    private void GhiLogMatDat(bool coMat, float loi)
    {
        if (!ghiLogChanDoan || Time.time - thoiDiemLogMat < 0.5f) return;
        thoiDiemLogMat = Time.time;

        int n = Physics.RaycastNonAlloc(
            rb.position + Vector3.up * doCaoTiaRay, Vector3.down, bufferMep, 6f, ~0,
            QueryTriggerInteraction.Ignore);

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = bufferMep[i];
            if (h.collider == null) continue;
            sb.Append(DuongDan(h.collider.transform))
              .Append(" (y=").Append(h.point.y.ToString("F2"))
              .Append(", ny=").Append(h.normal.y.ToString("F2"))
              .Append(", matDuong=").Append(LaMatDuong(h.collider)).Append("); ");
        }

        Debug.LogWarning($"[XE MẤT ĐƯỜNG] pos={rb.position} tốc độ={tocDoDangChay:F1} coMat={coMat} loi={loi:F2} soTuong={soTuong} | dưới chân: {sb}");
    }

    // Không GetComponent mỗi frame (trong Editor GetComponent trả null còn sinh rác): chỉ tra lại khi ô được chọn đổi
    private static bool DangGoChu()
    {
        if (MultiplayerAllInOne.DangGoChat) return true;

        EventSystem es = EventSystem.current;
        if (es == null) return false;

        GameObject sel = es.currentSelectedGameObject;
        if (sel == null)
        {
            selCache = null;
            selInput = null;
            selTmp = null;
            return false;
        }

        if (sel != selCache)
        {
            selCache = sel;
            sel.TryGetComponent(out selInput);
            sel.TryGetComponent(out selTmp);
        }

        return (selInput != null && selInput.isFocused) || (selTmp != null && selTmp.isFocused);
    }

    private bool DuocPhepDieuKhien()
    {
        GameManager gm = LayGM();
        if (gm != null && (!gm.daBatDauDua || gm.daBiLoai || gm.ketThucGame || gm.winGame)) return false;
        return !DangGoChu();
    }

    private void Update()
    {
        if (!LaXeCuaMinh) return;

        if (!DuocPhepDieuKhien())
        {
            dangTangToc = false;
            dangLui = false;
            dauVaoRe = 0f;

            GameManager gm = LayGM();
            if (gm != null && (gm.ketThucGame || gm.winGame || gm.daBiLoai))
                xeDangBat = false;
            return;
        }

        if (!daLuuLucBatDau)
        {
            daLuuLucBatDau = true;
            LuuDiemXuatPhat();
        }

        float doc = Input.GetAxisRaw("Vertical");
        if (Input.GetKey(KeyCode.W)) doc = 1f;
        else if (Input.GetKey(KeyCode.S)) doc = -1f;

        float re = Input.GetAxisRaw("Horizontal");
        if (Input.GetKey(KeyCode.D)) re = 1f;
        else if (Input.GetKey(KeyCode.A)) re = -1f;
        dauVaoRe = re;

        dangTangToc = doc > 0.1f;
        dangLui = doc < -0.1f;

        if (dangTangToc) xeDangBat = true;

        if (Input.GetKeyDown(KeyCode.Space))
            thoiDiemBamNhay = Time.time;
    }

    private void FixedUpdate()
    {
        if (!LaXeCuaMinh || rb == null || rb.isKinematic) return;

        GameManager gm = LayGM();
        if (gm != null && gm.daBatDauDua && rb.position.y < docaoRoiNuoc)
        {
            HoiSinh();
            return;
        }

        bool coMat = DoMatDuong(out Vector3 phapTuyenRay, out float matY);

        float loi = (matY + chieuCaoPivot) - rb.position.y;
        if (coMat && Mathf.Abs(loi) > doLechToiDaTiaDo) coMat = false;

        float sauNhay = Time.time - thoiDiemNhayCuoi;
        bool vuaNhay = sauNhay < 0.2f || (sauNhay < 0.8f && rb.linearVelocity.y > 1f);

        // Tia hụt 1-2 frame ở khe nối đường thì không bỏ bám (tránh bật/tắt gravity gây khựng)
        if (coMat) thoiDiemThayMat = Time.time;
        bool giuBam = !coMat && Time.time - thoiDiemThayMat < thoiGianGiuBam;

        dangBamDat = (coMat || giuBam) && !vuaNhay;

        if (ghiLogChanDoan && daBamDatTruoc && !dangBamDat && !vuaNhay) GhiLogMatDat(coMat, loi);
        daBamDatTruoc = dangBamDat;

        phapTuyenDat = coMat
            ? Vector3.Slerp(phapTuyenDat, phapTuyenRay, doMuotPhapTuyen).normalized
            : Vector3.Slerp(phapTuyenDat, Vector3.up, 0.2f).normalized;

        suaCaoDo = 0f;
        if (dangBamDat)
        {
            thoiDiemChamDat = Time.time;
            if (coMat && Mathf.Abs(loi) > 0.02f)   // hút xuống nhanh, nâng lên chậm (hết nảy)
                suaCaoDo = Mathf.Clamp(loi * heSoHutDat, -toiDaHutDat, toiDaNangLen);
        }

        rb.useGravity = !dangBamDat;
        rb.angularVelocity = Vector3.zero;

        // Rơi nhanh hơn lúc đi xuống để cú nhảy gọn và vào đường nhanh sau khi qua vật cản
        if (!dangBamDat && heSoRoiNhanh > 1f && rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (heSoRoiNhanh - 1f), ForceMode.Acceleration);

        if (!dangBamDat && !vuaNhay && rb.linearVelocity.y > vanTocLenToiDa)
        {
            Vector3 vv = rb.linearVelocity;
            rb.linearVelocity = new Vector3(vv.x, vanTocLenToiDa, vv.z);
        }

        DiChuyenXe();
        ReXe();
        PhanhXe();
        NhayXe();

        soTuong = 0;
        soDoc = 0;
        chamSanVat = false;
    }

    public void DiChuyenXe()
    {
        float dt = Time.fixedDeltaTime;
        float tocDoMucTieu = 0f;

        if (dangTangToc) tocDoMucTieu = tocDoXe * heSoTangToc;
        else if (dangLui) tocDoMucTieu = -tocDoXe * 0.5f;
        else if (xeDangBat) tocDoMucTieu = tocDoXe;

        Vector3 huongTien = rb.rotation * Vector3.right;
        Vector3 vHienTai = rb.linearVelocity;
        float tocDoThucTe = Vector3.Dot(vHienTai, huongTien);

        if (Mathf.Abs(tocDoDangChay - tocDoThucTe) > NGUONG_BI_CHAN) thoiGianBiChan += dt;
        else thoiGianBiChan = 0f;

        if (thoiGianBiChan > THOI_GIAN_BI_CHAN)
        {
            tocDoDangChay = tocDoThucTe;
            thoiGianBiChan = 0f;
        }

        float tocDoThayDoi = (Mathf.Abs(tocDoMucTieu) > Mathf.Abs(tocDoDangChay) ? giaToc * 3f : giaToc * 5f);
        tocDoDangChay = Mathf.MoveTowards(tocDoDangChay, tocDoMucTieu, tocDoThayDoi * dt * 3f);

        QuetVatCanCungPhiaTruoc(huongTien, dt);
        ChanMepDuong(huongTien);

        if (dangBamDat)
        {
            // Vận tốc đi theo mặt đường (có thành phần Y theo độ dốc)
            Vector3 v = Vector3.ProjectOnPlane(huongTien, phapTuyenDat).normalized * tocDoDangChay;
            Vector2 ngang = new Vector2(v.x, v.z);
            Vector3 vn = TruVaoTuong(new Vector3(v.x, 0f, v.z));
            float tiLe = ngang.sqrMagnitude > 1e-4f ? new Vector2(vn.x, vn.z).magnitude / ngang.magnitude : 0f;

            // Bị chặn thì giảm tốc độ nội bộ, hết cảnh "dính" đẩy vào vật
            if (soTuong > 0) tocDoDangChay = Mathf.Lerp(tocDoDangChay, tocDoDangChay * tiLe, 0.5f);

            rb.linearVelocity = new Vector3(vn.x, v.y * tiLe + suaCaoDo, vn.z);
        }
        else
        {
            Vector3 v = huongTien * tocDoDangChay;
            v.y = 0f;
            Vector3 vn = TruVaoTuong(v);

            if (soTuong > 0 && v.sqrMagnitude > 1e-4f)
                tocDoDangChay = Mathf.Lerp(tocDoDangChay, tocDoDangChay * (vn.magnitude / v.magnitude), 0.5f);

            Vector3 vf = new Vector3(vn.x, vHienTai.y, vn.z);

            // Mặt dốc/gồ ghề: trượt dọc theo mặt thay vì đẩy vào
            for (int i = 0; i < soDoc; i++)
            {
                float d = Vector3.Dot(vf, phapTuyenDoc[i]);
                if (d < 0f) vf -= phapTuyenDoc[i] * d;
            }

            // Tiếp đất lên vật khác: không cho bật ngược lên
            bool vuaNhay = Time.time - thoiDiemNhayCuoi < 0.2f;
            if (chamSanVat && !vuaNhay && vf.y > 0f) vf.y = 0f;

            rb.linearVelocity = vf;
        }
    }

    public void ReXe()
    {
        float huongMucTieu = 0f;

        if (Mathf.Abs(tocDoDangChay) >= 0.1f && !Mathf.Approximately(dauVaoRe, 0f))
            huongMucTieu = tocDoDangChay < 0f ? -dauVaoRe : dauVaoRe;

        gocReHienTai = Mathf.MoveTowards(gocReHienTai, huongMucTieu, doMuotLai * Time.fixedDeltaTime);

        if (Mathf.Abs(gocReHienTai) < 0.001f) return;

        Quaternion re = Quaternion.Euler(0f, gocReHienTai * lucReXe * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * re);
    }
    public void PhanhXe()
    {
        if (hieuUngPhanh == null) return;
        bool bat = dangLui && Mathf.Abs(tocDoDangChay) > 1f;
        if (hieuUngPhanh.activeSelf != bat) hieuUngPhanh.SetActive(bat);
    }

    private void NhayXe()
    {
        bool duocBam = Time.time - thoiDiemBamNhay <= thoiGianDemNhay;
        bool duocChamDat = Time.time - thoiDiemChamDat <= thoiGianCoyote;

        if (!duocBam || !duocChamDat) return;

        thoiDiemBamNhay = -10f;
        thoiDiemChamDat = -10f;
        thoiDiemNhayCuoi = Time.time;
        dangBamDat = false;
        rb.useGravity = true;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, lucNhay, v.z);
    }
}