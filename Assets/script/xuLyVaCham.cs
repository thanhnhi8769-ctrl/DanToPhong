using UnityEngine;
using Photon.Pun;

public class XuLyVaChamChoXe : MonoBehaviourPun
{
    private Rigidbody rb;

    private bool LaXeCuaMinh
    {
        get { return !PhotonNetwork.IsConnected || photonView == null || photonView.IsMine; }
    }

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
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

    // Vật cản (Obstacle / Barricade) là vật cứng: không bỏ va chạm nữa.
    // Việc chặn xe do scooter.cs xử lý.
}