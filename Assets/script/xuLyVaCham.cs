using UnityEngine;
using Photon.Pun;

public class XuLyVaChamChoXe : MonoBehaviourPun
{
    private Rigidbody rb;
    private Collider xeCol;

    private bool LaXeCuaMinh
    {
        get { return !PhotonNetwork.IsConnected || photonView == null || photonView.IsMine; }
    }

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        xeCol = GetComponent<Collider>();

        if (rb != null)
        {
            // Chạy mượt khung hình, không lún mặt đường gây khựng xe
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. CheckPoint: ăn đồng xu cộng thời gian
        if (other.CompareTag("CheckPoint"))
        {
            if (!other.gameObject.activeSelf) return;

            other.gameObject.SetActive(false);

            if (LaXeCuaMinh && GameManager.Instance != null)
                GameManager.Instance.QuaCheckPoint();

            return;
        }

        // 2. WinPoint: chạm vạch đích
        if (other.CompareTag("WinPoint"))
        {
            if (LaXeCuaMinh && GameManager.Instance != null)
                GameManager.Instance.QuaWinPoint(other);
            return;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        // Chống kẹt xe & chống bị hất văng xuống biển khi đâm trúng vật cản
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.name.Contains("Barricade"))
        {
            if (xeCol != null && collision.collider != null)
            {
                Physics.IgnoreCollision(xeCol, collision.collider, true);
            }

            // Chỉ làm chậm xe nhẹ 20% thay vì chặn đứng hoặc đẩy bay xe
            if (rb != null)
            {
                rb.linearVelocity *= 0.8f;
            }
        }
    }
}