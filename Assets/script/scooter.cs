using UnityEngine;
using Photon.Pun; // Thư viện Photon
using Unity.Cinemachine; // Thư viện Cinemachine cho Unity 6

public class scooter : MonoBehaviour
{
    [Header("Di chuyển")]
    [SerializeField] private float tocDoXe = 15f;
    [SerializeField] private float heSoTangToc = 1.6f;
    [SerializeField] private float lucReXe = 50f;
    [SerializeField] private float giaToc = 5f;

    [Header("Phanh & Lùi")]
    [SerializeField] private float doGatPhanh = 5f;
    [SerializeField] private GameObject hieuUngPhanh;

    [Header("Nhảy")]
    [SerializeField] private float lucNhay = 8f;
    [SerializeField] private float thoiGianCoyote = 0.15f;
    [SerializeField] private float thoiGianDemNhay = 0.15f;
    [SerializeField] private bool hienLogDebug = false;

    private bool xeDangBat = false;
    private bool dangPhanh = false;
    private bool dangTangToc = false;
    private bool dangLui = false;
    private float dauVaoRe;
    private float tocDoDangChay = 0f;

    private float thoiDiemChamDat = -10f;
    private float thoiDiemBamNhay = -10f;

    private Rigidbody rb;
    private PhotonView pv;
    private MeshRenderer mrIcon;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();

        if (hieuUngPhanh != null)
        {
            hieuUngPhanh.SetActive(false);
        }

        // Xóa MinimapIcon cũ (nếu có) để tạo mới dạng Mũi Tên định hướng GPS
        foreach (Transform con in GetComponentsInChildren<Transform>(true))
        {
            if (con.name.Trim() == "MinimapIcon")
            {
                Destroy(con.gameObject);
            }
        }

        // Tạo hình Mũi Tên bằng Mesh phẳng hướng thẳng theo đầu xe (trục +X của scooter)
        GameObject muiTenObj = new GameObject("MinimapIcon");
        muiTenObj.transform.SetParent(this.transform);
        muiTenObj.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        muiTenObj.transform.localRotation = Quaternion.identity;
        muiTenObj.transform.localScale = new Vector3(4.5f, 1f, 4.5f);

        int layerMinimap = LayerMask.NameToLayer("Minimap");
        if (layerMinimap != -1)
        {
            muiTenObj.layer = layerMinimap;
        }

        MeshFilter mf = muiTenObj.AddComponent<MeshFilter>();
        mrIcon = muiTenObj.AddComponent<MeshRenderer>();

        // Dựng hình mũi tên GPS (mũi nhọn hướng về +X, đuôi khoét chữ V)
        Mesh meshMuiTen = new Mesh();
        meshMuiTen.vertices = new Vector3[]
        {
            new Vector3( 1.2f, 0f,  0.0f), // 0: Đỉnh nhọn phía trước đầu xe
            new Vector3(-0.9f, 0f,  0.85f),// 1: Cánh trái
            new Vector3(-0.4f, 0f,  0.0f), // 2: Điểm lõm ở đuôi
            new Vector3(-0.9f, 0f, -0.85f) // 3: Cánh phải
        };
        meshMuiTen.triangles = new int[]
        {
            0, 2, 1, // Nửa trái mũi tên (mặt hướng lên trời +Y)
            0, 3, 2  // Nửa phải mũi tên (mặt hướng lên trời +Y)
        };
        meshMuiTen.RecalculateNormals();
        mf.mesh = meshMuiTen;

        // Cấp phát Material độc lập cho từng xe và tô màu chuẩn URP
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
        mrIcon.material = new Material(unlitShader);

        CapNhatMauMuiTen();
        Invoke(nameof(CapNhatMauMuiTen), 0.25f);

        bool laXeCuaMinh = !PhotonNetwork.IsConnected || (pv != null && pv.IsMine);

        if (laXeCuaMinh)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // 1. Gắn CinemachineCamera chính bám theo xe
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                Transform diemBamCam = transform.Find("pointFollowTarget");
                cam.Target.TrackingTarget = (diemBamCam != null) ? diemBamCam : this.transform;
            }

            // 2. Gắn Camera Minimap bám theo xe của mình
            MinimapController minimap = FindFirstObjectByType<MinimapController>();
            if (minimap != null)
            {
                minimap.targetXeCuaMinh = this.transform;
            }
        }
        else
        {
            // Nếu là xe của người chơi khác: Tắt vật lý cục bộ để nhận tọa độ mượt từ PhotonTransformView
            rb.isKinematic = true;
        }
    }

    private void CapNhatMauMuiTen()
    {
        if (mrIcon == null) return;

        bool laXeCuaMinh = PhotonNetwork.IsConnected
            ? (pv != null && pv.IsMine)
            : true;

        Color mauDanhDau = laXeCuaMinh ? Color.green : Color.red;
        mrIcon.material.SetColor("_BaseColor", mauDanhDau);
        mrIcon.material.color = mauDanhDau;
    }

    // Xử lý ăn đồng xu Tag "CheckPoint": biến mất đồng xu trên cả 2 máy & cộng +5s cho người ăn
    private void XuLyAnDongXu(GameObject objVaCham)
    {
        if (objVaCham == null || !objVaCham.activeSelf) return;

        if (objVaCham.CompareTag("CheckPoint"))
        {
            if (!PhotonNetwork.IsConnected || (pv != null && pv.IsMine))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.QuaCheckPoint();
                }
            }

            objVaCham.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        XuLyAnDongXu(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        XuLyAnDongXu(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (pv != null && !pv.IsMine) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                thoiDiemChamDat = Time.time;
                return;
            }
        }
    }

    private void Update()
    {
        // Chặn nhận phím bấm nếu chiếc xe này không phải của mình
        if (pv != null && !pv.IsMine) return;

        // Khóa điều khiển xe cho đến khi TẤT CẢ người chơi trong phòng đều bấm Bắt Đầu Đua
        if (GameManager.Instance != null && !GameManager.Instance.daBatDauDua)
        {
            dangTangToc = false;
            dangLui = false;
            dauVaoRe = 0f;
            return;
        }

        // Mũi tên lên hoặc W: tiến tới + tăng tốc
        dangTangToc = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        if (dangTangToc)
        {
            xeDangBat = true;
        }

        // Mũi tên xuống hoặc S: Chạy lùi
        dangLui = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        // A/D hoặc mũi tên trái/phải: rẽ
        dauVaoRe = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dauVaoRe += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dauVaoRe -= 1f;

        // Space: nhảy
        if (Input.GetKeyDown(KeyCode.Space))
        {
            thoiDiemBamNhay = Time.time;
        }
    }

    private void FixedUpdate()
    {
        // Chặn tính toán vật lý nếu chiếc xe này không phải của mình
        if (pv != null && !pv.IsMine) return;

        rb.angularVelocity = Vector3.zero;

        DiChuyenXe();
        ReXe();
        PhanhXe();
        NhayXe();
    }

    public void DiChuyenXe()
    {
        float tocDoMucTieu = 0f;

        if (dangTangToc)
        {
            tocDoMucTieu = tocDoXe * heSoTangToc;
        }
        else if (dangLui)
        {
            tocDoMucTieu = -tocDoXe * 0.5f;
        }
        else if (xeDangBat)
        {
            tocDoMucTieu = tocDoXe;
        }

        tocDoDangChay = Mathf.Lerp(tocDoDangChay, tocDoMucTieu, giaToc * Time.fixedDeltaTime);

        Vector3 huongDiChuyen = transform.right * tocDoDangChay;
        rb.linearVelocity = new Vector3(huongDiChuyen.x, rb.linearVelocity.y, huongDiChuyen.z);
    }

    public void ReXe()
    {
        if (Mathf.Abs(tocDoDangChay) < 0.1f || dauVaoRe == 0f) return;

        float huongReThucTe = (tocDoDangChay < 0) ? -dauVaoRe : dauVaoRe;

        Quaternion re = Quaternion.Euler(0f, huongReThucTe * lucReXe * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * re);
    }

    public void PhanhXe()
    {
        if (hieuUngPhanh != null)
        {
            hieuUngPhanh.SetActive(dangLui && Mathf.Abs(tocDoDangChay) > 1f);
        }
    }

    private void NhayXe()
    {
        bool duocBamGanDay = Time.time - thoiDiemBamNhay <= thoiGianDemNhay;
        bool duocChamDatGanDay = Time.time - thoiDiemChamDat <= thoiGianCoyote;

        if (!duocBamGanDay || !duocChamDatGanDay) return;

        thoiDiemBamNhay = -10f;
        thoiDiemChamDat = -10f;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * lucNhay, ForceMode.Impulse);
    }
}