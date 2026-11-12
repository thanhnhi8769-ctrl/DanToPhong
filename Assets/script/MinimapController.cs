using UnityEngine;
using Photon.Pun;

public class MinimapController : MonoBehaviour
{
    [Header("Cài đặt bám theo xe")]
    public Transform targetXeCuaMinh;
    public float doCaoCamera = 40f;
    public bool xoayTheoHuongXe = false;

    [Header("Cài đặt Zoom")]
    public float mucZoomHienTai = 35f;
    public float zoomNhoNhat = 15f; // Zoom gần
    public float zoomLonNhat = 90f; // Zoom xa
    public float tocDoZoomPhim = 25f;

    private Camera minimapCam;

    void Start()
    {
        minimapCam = GetComponent<Camera>();
        ApDungZoom();
    }

    void LateUpdate()
    {
        if (targetXeCuaMinh == null)
        {
            TimXeCuaMinh();
            return;
        }

        // 1. Bám theo vị trí xe từ trên cao nhìn xuống
        Vector3 viTriMoi = targetXeCuaMinh.position;
        viTriMoi.y += doCaoCamera;
        transform.position = viTriMoi;

        if (xoayTheoHuongXe)
        {
            transform.rotation = Quaternion.Euler(90f, targetXeCuaMinh.eulerAngles.y - 90f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // 2. Xử lý Zoom bằng con lăn chuột hoặc phím + / -
        float cuonChuot = Input.mouseScrollDelta.y;
        if (cuonChuot != 0f)
        {
            mucZoomHienTai -= cuonChuot * 5f;
            ApDungZoom();
        }

        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Z))
        {
            mucZoomHienTai -= tocDoZoomPhim * Time.deltaTime;
            ApDungZoom();
        }
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.X))
        {
            mucZoomHienTai += tocDoZoomPhim * Time.deltaTime;
            ApDungZoom();
        }
    }

    // Gắn hàm này vào nút dấu "+" trên góc Minimap
    public void ZoomIn()
    {
        mucZoomHienTai -= 10f;
        ApDungZoom();
    }

    // Gắn hàm này vào nút dấu "-" trên góc Minimap
    public void ZoomOut()
    {
        mucZoomHienTai += 10f;
        ApDungZoom();
    }

    private void ApDungZoom()
    {
        mucZoomHienTai = Mathf.Clamp(mucZoomHienTai, zoomNhoNhat, zoomLonNhat);

        if (minimapCam != null && minimapCam.orthographic)
        {
            minimapCam.orthographicSize = mucZoomHienTai;
        }
        else
        {
            // Nếu Camera đang ở chế độ Perspective thì thay đổi độ cao Y để zoom
            doCaoCamera = mucZoomHienTai;
        }
    }

    void TimXeCuaMinh()
    {
        scooter[] tatCaXe = FindObjectsByType<scooter>(FindObjectsSortMode.None);
        foreach (scooter xe in tatCaXe)
        {
            PhotonView pv = xe.GetComponent<PhotonView>();
            if (pv == null || pv.IsMine)
            {
                targetXeCuaMinh = xe.transform;
                break;
            }
        }
    }
}