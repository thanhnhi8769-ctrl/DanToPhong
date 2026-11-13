using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class scooter : MonoBehaviour
{
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

    private bool xeDangBat = false;
    private bool dangLui = false;
    private bool dangTangToc = false;
    private float dauVaoRe;
    private float gocReHienTai = 0f;
    private float tocDoDangChay = 0f;

    private float thoiDiemChamDat = -10f;
    private float thoiDiemBamNhay = -10f;

    private Rigidbody rb;
    private PhotonView pv;
    private MeshRenderer mrIcon;
    private Mesh meshMuiTen;
    private Material matMuiTen;

    private bool LaXeCuaMinh
    {
        get { return !PhotonNetwork.IsConnected || pv == null || pv.IsMine; }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();

        if (hieuUngPhanh != null) hieuUngPhanh.SetActive(false);

        TaoMuiTenMinimap();

        if (LaXeCuaMinh)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                Transform diemBamCam = transform.Find("pointFollowTarget");
                cam.Target.TrackingTarget = (diemBamCam != null) ? diemBamCam : transform;
            }

            MinimapController minimap = FindFirstObjectByType<MinimapController>();
            if (minimap != null) minimap.targetXeCuaMinh = transform;
        }
        else
        {
            // Xe người khác: tắt vật lý cục bộ, nhận toạ độ từ PhotonTransformView
            rb.isKinematic = true;
        }
    }

    private void OnDestroy()
    {
        if (matMuiTen != null) Destroy(matMuiTen);
        if (meshMuiTen != null) Destroy(meshMuiTen);
    }

    // =========================================================================
    // MŨI TÊN MINIMAP
    // =========================================================================
    private void TaoMuiTenMinimap()
    {
        foreach (Transform con in GetComponentsInChildren<Transform>(true))
        {
            if (con != transform && con.name.Trim() == "MinimapIcon") Destroy(con.gameObject);
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
        // PhotonView có thể chưa gán chủ sở hữu ngay -> cập nhật lại 1 lần
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
    // VA CHẠM (CheckPoint / WinPoint xử lý ở XuLyVaChamChoXe)
    // =========================================================================
    private bool LaNuoc(GameObject go)
    {
        return go.name.Contains("Sea_Plane") || go.CompareTag("Sea_Plane") || go.CompareTag("Water");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!LaXeCuaMinh) return;

        if (LaNuoc(other.gameObject) && GameManager.Instance != null)
            GameManager.Instance.ChamNuocTruThoiGian();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        if (LaNuoc(collision.gameObject) && GameManager.Instance != null)
            GameManager.Instance.ChamNuocTruThoiGian();

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                thoiDiemChamDat = Time.time;
                return;
            }
        }
    }

    // =========================================================================
    // INPUT
    // =========================================================================
    private void Update()
    {
        if (!LaXeCuaMinh) return;

        // Khoá điều khiển đến khi tất cả người chơi bấm Bắt Đầu Đua
        if (GameManager.Instance != null && !GameManager.Instance.daBatDauDua)
        {
            dangTangToc = false;
            dangLui = false;
            dauVaoRe = 0f;
            return;
        }

        float doc = Input.GetAxisRaw("Vertical");   // W/S, mũi tên lên/xuống
        dauVaoRe = Input.GetAxisRaw("Horizontal");  // A/D, mũi tên trái/phải

        dangTangToc = doc > 0.1f;
        dangLui = doc < -0.1f;
        if (dangTangToc) xeDangBat = true;

        if (Input.GetKeyDown(KeyCode.Space)) thoiDiemBamNhay = Time.time;
    }

    // =========================================================================
    // VẬT LÝ
    // =========================================================================
    private void FixedUpdate()
    {
        if (!LaXeCuaMinh) return;

        rb.angularVelocity = Vector3.zero;

        DiChuyenXe();
        ReXe();
        PhanhXe();
        NhayXe();
    }

    public void DiChuyenXe()
    {
        float tocDoMucTieu = 0f;

        if (dangTangToc) tocDoMucTieu = tocDoXe * heSoTangToc;
        else if (dangLui) tocDoMucTieu = -tocDoXe * 0.5f;
        else if (xeDangBat) tocDoMucTieu = tocDoXe;

        // Đồng bộ lại tốc độ nếu xe bị hãm do va chạm thật sự
        Vector3 vHienTai = rb.linearVelocity;
        float tocDoThucTe = Vector3.Dot(new Vector3(vHienTai.x, 0f, vHienTai.z), transform.right);
        if (Mathf.Abs(tocDoDangChay - tocDoThucTe) > 4f)
        {
            tocDoDangChay = tocDoThucTe;
        }

        tocDoDangChay = Mathf.Lerp(tocDoDangChay, tocDoMucTieu, giaToc * Time.fixedDeltaTime);

        Vector3 huong = transform.right * tocDoDangChay;
        rb.linearVelocity = new Vector3(huong.x, vHienTai.y, huong.z);
    }

    public void ReXe()
    {
        float huongMucTieu = 0f;
        if (Mathf.Abs(tocDoDangChay) >= 0.1f && !Mathf.Approximately(dauVaoRe, 0f))
        {
            huongMucTieu = (tocDoDangChay < 0f) ? -dauVaoRe : dauVaoRe;
        }

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

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        rb.AddForce(Vector3.up * lucNhay, ForceMode.Impulse);
    }
}