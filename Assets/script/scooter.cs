using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class scooter : MonoBehaviour, IPunInstantiateMagicCallback
{
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
    [SerializeField] private float lucReXe = 85f;
    [SerializeField] private float giaToc = 6f;
    [SerializeField] private float doMuotLai = 10f;

    [Header("Phanh & Lùi")]
    [SerializeField] private GameObject hieuUngPhanh;

    [Header("Nhảy")]
    [SerializeField] private float lucNhay = 8f;
    [SerializeField] private float thoiGianCoyote = 0.15f;
    [SerializeField] private float thoiGianDemNhay = 0.15f;

    [Header("Bám đường (tia dò)")]
    [SerializeField] private float maxDepenetration = 2f;
    [SerializeField] private float doCaoTiaRay = 0.7f;
    [SerializeField] private float khoangBamDat = 0.9f;
    [SerializeField] private float doMuotPhapTuyen = 0.35f;
    [SerializeField, Tooltip("Độ cứng giữ độ cao so với mặt đường")]
    private float heSoHutDat = 12f;
    [SerializeField, Tooltip("Tốc độ nhấc/hạ xe tối đa để theo mặt đường (m/s)")]
    private float toiDaHutDat = 4f;
    [SerializeField, Tooltip("Tia dò thấy mặt đường lệch quá mức này thì bỏ qua")]
    private float doLechToiDaTiaDo = 0.6f;

    [Header("Mẫu xe (thứ tự = thứ tự trong Shop)")]
    [SerializeField, Tooltip("Để trống = tự dò từ cây prefab. Mẫu nào hiện thiếu bánh/thân thì khai báo ở đây: mỗi phần tử liệt kê MỌI object cần bật cho mẫu đó.")]
    private MauXe[] cacMauXe;
    [SerializeField, Tooltip("(Cũ) Dùng khi 'Cac Mau Xe' để trống")]
    private Transform[] danhSachMauXe;
    [SerializeField, Tooltip("Bật mọi mesh thuộc mẫu đang chọn (phòng khi prefab để ẩn một số phần)")]
    private bool batMoiMeshCuaMau = true;

    [Header("Chạy mượt")]
    [SerializeField] private bool taoMaterialKhongMaSat = true;
    [SerializeField] private bool boMeshColliderTrenXe = true;
    [SerializeField] private bool boQuaVaChamMatDuong = false;
    [SerializeField] private float doCaoGoBoQua = 0.12f;

    [Tooltip("Xe xuyên qua vật có tên chứa các từ này, cách nhau bằng dấu phẩy.")]
    [SerializeField] private string tuKhoaBoQuaVaCham = "obstacle,barricade,cone,prop,decor,tree,grass";

    [Header("Hồi sinh khi rơi nước")]
    [SerializeField] private float nhacLenKhiHoiSinh = 0.5f;
    [SerializeField] private float thoiGianChoHoiSinh = 1f;
    [SerializeField] private float docaoRoiNuoc = -0.3f;
    [SerializeField] private float bienChamNuoc = 0.5f;

    [Header("Mô hình chạm đường")]
    [SerializeField] private bool tuHaMoHinhChamDat = false;
    [SerializeField] private float moHinhHaThem = 0f;

    [Header("Chẩn đoán")]
    [SerializeField] private bool ghiLogVaCham = true;

    private const float NGUONG_BI_CHAN = 5f;
    private const float THOI_GIAN_BI_CHAN = 0.3f;

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

    private Vector3 phapTuyenDat = Vector3.up;
    private bool dangBamDat = false;
    private readonly RaycastHit[] bufferRay = new RaycastHit[16];
    private float chieuCaoPivot = 0f;
    private float suaCaoDo = 0f;

    private readonly Vector3[] phapTuyenTuong = new Vector3[8];
    private int soTuong = 0;

    private Vector3 viTriXuatPhat;
    private Quaternion huongXuatPhat;
    private float thoiDiemHoiSinh = -10f;
    private bool daLuuLucBatDau = false;
    private int soLanCanMoHinh = 0;
    private float thoiDiemLogChan = -10f;

    private string[] tuKhoaVat = new string[0];
    private readonly Dictionary<Collider, bool> cacheMatDuong = new Dictionary<Collider, bool>();

    private List<GameObject[]> mauXeRuntime;
    private int mauTuMang = -1;
    private int mauDangDung = -1;

    private Rigidbody rb;
    private PhotonView pv;
    private Collider[] tatCaColliderXe;
    private MeshRenderer mrIcon;
    private Mesh meshMuiTen;
    private Material matMuiTen;

    private bool LaXeCuaMinh
    {
        get { return !PhotonNetwork.IsConnected || pv == null || pv.IsMine; }
    }

    public float DoNghieng { get { return gocReHienTai; } }
    public float TocDoToiDa { get { return tocDoXe; } }
    public bool DangLui { get { return dangLui; } }
    public int SoMauXe { get { return 2; } }
    public int MauDangDung { get { return mauDangDung; } }

    // =========================================================================
    // MẪU XE
    // =========================================================================
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView != null ? info.photonView.InstantiationData : null;
        if (data == null || data.Length == 0) return;

        mauTuMang = System.Convert.ToInt32(data[0]);
        ApDungMauXe(mauTuMang);
    }

    public void ApDungMauXe(int indexXe)
    {
        if (indexXe < 0) indexXe = 0;
        mauDangDung = indexXe;

        GameNetworkManager gnm = FindFirstObjectByType<GameNetworkManager>();
        float kichThuoc = (gnm != null) ? gnm.kichThuocXeChuan : 1.6f;

        GameNetworkManager.KichHoatDungMauXe(gameObject, indexXe, kichThuoc);
    }

    // =========================================================================
    // KHỞI TẠO
    // =========================================================================
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();

        DonColliderXe();

        if (!string.IsNullOrWhiteSpace(tuKhoaBoQuaVaCham))
        {
            tuKhoaVat = tuKhoaBoQuaVaCham.ToLower().Split(',');
            for (int i = 0; i < tuKhoaVat.Length; i++) tuKhoaVat[i] = tuKhoaVat[i].Trim();
        }

        if (hieuUngPhanh != null) hieuUngPhanh.SetActive(false);

        TaoMuiTenMinimap();
        DamBaoCameraThayXe();

        if (tuHaMoHinhChamDat)
            Invoke(nameof(CanMoHinhChamDat), 1.5f);

        if (LaXeCuaMinh)
        {
            int indexTrangBi = mauTuMang >= 0 ? mauTuMang : PlayerInventory.GetEquippedIndex("Scooter");
            ApDungMauXe(indexTrangBi);
            LuuDiemXuatPhat();

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxDepenetrationVelocity = maxDepenetration;

            TinhChieuCaoPivot();
            ThietLapColliderXe();
            BoQuaVatCanNho();
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

            if (mauTuMang >= 0)
                ApDungMauXe(mauTuMang);
            else if (pv != null && pv.Owner != null && pv.Owner.CustomProperties.TryGetValue("Scooter", out object val))
                ApDungMauXe(System.Convert.ToInt32(val));
        }
    }

    private void OnDestroy()
    {
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

        int layerMinimap = LayerMask.NameToLayer("Minimap");
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

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        tocDoDangChay = 0f;
        gocReHienTai = 0f;
        thoiGianBiChan = 0f;
        soTuong = 0;
        xeDangBat = false;
        dangBamDat = false;
        phapTuyenDat = Vector3.up;
        thoiDiemChamDat = -10f;
        thoiDiemBamNhay = -10f;

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
        muiTenObj.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        muiTenObj.transform.localRotation = Quaternion.identity;
        muiTenObj.transform.localScale = new Vector3(4.5f, 1f, 4.5f);

        int layerMinimap = LayerMask.NameToLayer("Minimap");
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

    private bool LaNuoc(GameObject go)
    {
        string ten = go.name;
        return ten.Contains("Sea_Plane") || ten.Contains("Water");
    }

    private bool XeDaXuongNuoc()
    {
        return !dangBamDat && transform.position.y < docaoRoiNuoc + bienChamNuoc;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!LaXeCuaMinh) return;
        if (LaNuoc(other.gameObject) && XeDaXuongNuoc()) HoiSinh();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!LaXeCuaMinh) return;
        if (LaNuoc(other.gameObject) && XeDaXuongNuoc()) HoiSinh();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        if (LaNuoc(collision.gameObject) && XeDaXuongNuoc())
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

        GhiNhanTuong(collision);
        GhiLogNeuBiChan(collision);
    }

    private bool LaGoThap(ContactPoint cp)
    {
        float duoiXe = rb.position.y - chieuCaoPivot;
        return cp.point.y < duoiXe + doCaoGoBoQua;
    }

    private void GhiNhanTuong(Collision c)
    {
        if (LaNuoc(c.gameObject)) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            ContactPoint cp = c.GetContact(i);
            Vector3 n = cp.normal;
            if (n.y > 0.5f) continue;
            if (LaGoThap(cp)) continue;

            n.y = 0f;
            if (n.sqrMagnitude < 0.01f) continue;
            n.Normalize();

            if (soTuong < phapTuyenTuong.Length)
                phapTuyenTuong[soTuong++] = n;
        }
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
        if (!ghiLogVaCham || Time.time - thoiDiemLogChan < 0.5f) return;
        if (tocDoDangChay < 5f || LaNuoc(c.gameObject)) return;

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

        PhysicsMaterial mat = new PhysicsMaterial("ScooterRuntime")
        {
            bounciness = 0f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        foreach (Collider c in tatCaColliderXe)
        {
            if (c != null && !c.isTrigger)
                c.sharedMaterial = mat;
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

    private void BoQuaVatCanNho()
    {
        if (tuKhoaVat.Length == 0) return;

        foreach (Collider vat in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!ColliderDungDuoc(vat) || vat.isTrigger) continue;
            if (vat.transform.IsChildOf(transform)) continue;
            if (vat.GetComponentInParent<Rigidbody>() != null) continue;

            string tenRieng = vat.gameObject.name.ToLower();
            if (tenRieng.Contains("road") || tenRieng.Contains("sea") || tenRieng.Contains("water"))
                continue;

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
    }

    private void XuyenMatDuong()
    {
        foreach (Collider vat in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!ColliderDungDuoc(vat) || vat.isTrigger) continue;
            if (vat.transform.IsChildOf(transform)) continue;
            if (vat.attachedRigidbody != null) continue;

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

    private static bool DangGoChu()
    {
        EventSystem es = EventSystem.current;
        if (es == null || es.currentSelectedGameObject == null) return false;

        GameObject sel = es.currentSelectedGameObject;

        InputField inp = sel.GetComponent<InputField>();
        if (inp != null && inp.isFocused) return true;

        TMP_InputField tmp = sel.GetComponent<TMP_InputField>();
        return tmp != null && tmp.isFocused;
    }

    private bool DuocPhepDieuKhien()
    {
        GameManager gm = GameManager.Instance;
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

            GameManager gm = GameManager.Instance;
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
        dauVaoRe = Input.GetAxisRaw("Horizontal");

        dangTangToc = doc > 0.1f;
        dangLui = doc < -0.1f;

        if (dangTangToc) xeDangBat = true;

        if (Input.GetKeyDown(KeyCode.Space))
            thoiDiemBamNhay = Time.time;
    }

    private void FixedUpdate()
    {
        if (!LaXeCuaMinh || rb == null || rb.isKinematic) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.daBatDauDua &&
            rb.position.y < docaoRoiNuoc)
        {
            HoiSinh();
            return;
        }

        bool coMat = DoMatDuong(out Vector3 phapTuyenRay, out float matY);

        float loi = (matY + chieuCaoPivot) - rb.position.y;
        if (coMat && Mathf.Abs(loi) > doLechToiDaTiaDo) coMat = false;

        float sauNhay = Time.time - thoiDiemNhayCuoi;
        bool vuaNhay = sauNhay < 0.2f || (sauNhay < 0.8f && rb.linearVelocity.y > 1f);

        dangBamDat = coMat && !vuaNhay;

        phapTuyenDat = coMat
            ? Vector3.Slerp(phapTuyenDat, phapTuyenRay, doMuotPhapTuyen).normalized
            : Vector3.Slerp(phapTuyenDat, Vector3.up, 0.2f).normalized;

        suaCaoDo = 0f;
        if (dangBamDat)
        {
            thoiDiemChamDat = Time.time;
            if (loi < -0.02f)
            {
                suaCaoDo = Mathf.Clamp(loi * heSoHutDat, -toiDaHutDat, 0f);
            }
        }

        rb.useGravity = !dangBamDat;
        rb.angularVelocity = Vector3.zero;

        DiChuyenXe();
        ReXe();
        PhanhXe();
        NhayXe();

        soTuong = 0;
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

        tocDoDangChay = Mathf.Lerp(tocDoDangChay, tocDoMucTieu, Mathf.Clamp01(giaToc * dt));

        if (dangBamDat)
        {
            Vector3 huongDoc = Vector3.ProjectOnPlane(huongTien, phapTuyenDat).normalized;
            huongDoc.y = 0f;
            huongDoc.Normalize();

            Vector3 v = TruVaoTuong(huongDoc * tocDoDangChay);
            float vY = Mathf.Clamp(suaCaoDo, -3f, 0f);
            rb.linearVelocity = new Vector3(v.x, vY, v.z);
        }
        else
        {
            Vector3 v = huongTien * tocDoDangChay;
            v.y = 0f;
            v = TruVaoTuong(v);
            rb.linearVelocity = new Vector3(v.x, vHienTai.y, v.z);
        }
    }

    public void ReXe()
    {
        float huongMucTieu = 0f;

        if (Mathf.Abs(tocDoDangChay) >= 0.1f && !Mathf.Approximately(dauVaoRe, 0f))
            huongMucTieu = tocDoDangChay < 0f ? -dauVaoRe : dauVaoRe;

        gocReHienTai = Mathf.Lerp(gocReHienTai, huongMucTieu, doMuotLai * Time.fixedDeltaTime);

        if (Mathf.Abs(gocReHienTai) < 0.001f) return;

        Quaternion re = Quaternion.Euler(0f, gocReHienTai * lucReXe * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * re);
    }

    public void PhanhXe()
    {
        if (hieuUngPhanh != null)
            hieuUngPhanh.SetActive(dangLui && Mathf.Abs(tocDoDangChay) > 1f);
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