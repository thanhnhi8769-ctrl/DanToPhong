using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(Camera))]
public class MinimapController : MonoBehaviour
{
    [Header("Camera Minimap")]
    [SerializeField] private Camera minimapCam;

    [Header("Cài đặt bám theo xe")]
    public Transform targetXeCuaMinh;
    public float doCaoCamera = 40f;
    public bool xoayTheoHuongXe = false;

    [Header("Cài đặt Zoom")]
    public float mucZoomHienTai = 35f;
    public float zoomNhoNhat = 15f;
    public float zoomLonNhat = 90f;
    public float tocDoZoomPhim = 25f;
    public float buocZoomNut = 10f;

    [Header("Hiệu ứng Zoom")]
    public bool zoomMuot = true;
    public float tocDoZoomMuot = 40f;

    [Header("Tự tìm xe")]
    public bool tuTimXe = true;
    public float thoiGianTimXe = 1f;

    private float zoomMucTieu;
    private float thoiDiemTimXe;
    private float zoomMacDinh;

    private void Awake()
    {
        if (minimapCam == null)
            minimapCam = GetComponent<Camera>();

        zoomMacDinh = mucZoomHienTai;

        mucZoomHienTai = Mathf.Clamp(
            mucZoomHienTai,
            zoomNhoNhat,
            zoomLonNhat
        );

        zoomMucTieu = mucZoomHienTai;
        ApDungZoomNgay();
    }

    private void Start()
    {
        TuDongGanNutZoom();
        TimXeCuaMinh();
    }

    // Tự động tìm BtnZoomIn và BtnZoomOut trong Scene và gắn sự kiện bấm bằng code
    private void TuDongGanNutZoom()
    {
        foreach (Button btn in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn == null) continue;
            string ten = btn.gameObject.name.Trim();

            if (ten == "BtnZoomIn")
            {
                btn.onClick.RemoveListener(ZoomIn);
                btn.onClick.AddListener(ZoomIn);
            }
            else if (ten == "BtnZoomOut")
            {
                btn.onClick.RemoveListener(ZoomOut);
                btn.onClick.AddListener(ZoomOut);
            }
        }
    }

    private void LateUpdate()
    {
        if (targetXeCuaMinh == null && tuTimXe)
        {
            if (Time.unscaledTime >= thoiDiemTimXe)
            {
                thoiDiemTimXe = Time.unscaledTime + thoiGianTimXe;
                TimXeCuaMinh();
            }
        }

        CapNhatViTriCamera();
        XuLyZoomPhim();
        CapNhatZoom();
    }

    private void CapNhatViTriCamera()
    {
        if (targetXeCuaMinh == null)
            return;

        Vector3 viTriMoi = targetXeCuaMinh.position;
        viTriMoi.y += doCaoCamera;

        transform.position = viTriMoi;

        if (xoayTheoHuongXe)
        {
            transform.rotation = Quaternion.Euler(
                90f,
                targetXeCuaMinh.eulerAngles.y - 90f,
                0f
            );
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void ZoomIn()
    {
        zoomMucTieu -= buocZoomNut;
        GioiHanZoom();
        CapNhatNgayNeuCan();
    }

    public void ZoomOut()
    {
        zoomMucTieu += buocZoomNut;
        GioiHanZoom();
        CapNhatNgayNeuCan();
    }

    private void XuLyZoomPhim()
    {
        float cuonChuot = Input.mouseScrollDelta.y;

        if (cuonChuot != 0f)
        {
            zoomMucTieu -= cuonChuot * 5f;
            GioiHanZoom();
        }

        if (Input.GetKey(KeyCode.Equals) ||
            Input.GetKey(KeyCode.KeypadPlus) ||
            Input.GetKey(KeyCode.Z))
        {
            zoomMucTieu -= tocDoZoomPhim * Time.unscaledDeltaTime;
            GioiHanZoom();
        }

        if (Input.GetKey(KeyCode.Minus) ||
            Input.GetKey(KeyCode.KeypadMinus) ||
            Input.GetKey(KeyCode.X))
        {
            zoomMucTieu += tocDoZoomPhim * Time.unscaledDeltaTime;
            GioiHanZoom();
        }
    }

    private void CapNhatZoom()
    {
        if (minimapCam == null)
            return;

        if (zoomMuot)
        {
            mucZoomHienTai = Mathf.MoveTowards(
                mucZoomHienTai,
                zoomMucTieu,
                tocDoZoomMuot * Time.unscaledDeltaTime
            );
        }
        else
        {
            mucZoomHienTai = zoomMucTieu;
        }

        ApDungZoomNgay();
    }

    private void ApDungZoomNgay()
    {
        if (minimapCam == null)
            return;

        mucZoomHienTai = Mathf.Clamp(
            mucZoomHienTai,
            zoomNhoNhat,
            zoomLonNhat
        );

        if (minimapCam.orthographic)
            minimapCam.orthographicSize = mucZoomHienTai;
        else
            minimapCam.fieldOfView = mucZoomHienTai;
    }

    private void CapNhatNgayNeuCan()
    {
        if (!zoomMuot)
        {
            mucZoomHienTai = zoomMucTieu;
            ApDungZoomNgay();
        }
    }

    private void GioiHanZoom()
    {
        zoomMucTieu = Mathf.Clamp(
            zoomMucTieu,
            zoomNhoNhat,
            zoomLonNhat
        );
    }

    public void TimXeCuaMinh()
    {
        if (targetXeCuaMinh != null)
            return;

        scooter[] tatCaXe = FindObjectsByType<scooter>(
            FindObjectsSortMode.None
        );

        foreach (scooter xe in tatCaXe)
        {
            if (xe == null)
                continue;

            PhotonView pv = xe.GetComponentInParent<PhotonView>();

            if (pv == null || pv.IsMine)
            {
                targetXeCuaMinh = xe.transform;
                Debug.Log("[Minimap] Đã tìm thấy xe của người chơi.");
                return;
            }
        }
    }

    public void ResetZoom()
    {
        zoomMucTieu = Mathf.Clamp(
            zoomMacDinh,
            zoomNhoNhat,
            zoomLonNhat
        );

        if (!zoomMuot)
        {
            mucZoomHienTai = zoomMucTieu;
            ApDungZoomNgay();
        }
    }
}