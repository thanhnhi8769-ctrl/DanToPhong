using UnityEngine;

public class XuLyVaChamChoXe : MonoBehaviour
{
    private Rigidbody rb;

    private bool LaXeCuaMinh
    {
        get
        {
            scooter controller = GetComponentInParent<scooter>();
            return controller == null || !controller.IsRemoteControlled;
        }
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
            // Each client has its own scene checkpoint; remote cars must not consume it locally.
            if (!LaXeCuaMinh) return;
            if (!other.gameObject.activeSelf) return;

            if (GameManager.Instance != null)
                GameManager.Instance.QuaCheckPoint(other);
            else
                other.gameObject.SetActive(false);

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
        // 3. Ra khỏi đường đua chạm Ground: trừ thời gian theo cấu hình ChamNuoc()
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (LaXeCuaMinh && GameManager.Instance != null)
            {
                GameManager.Instance.ChamNuoc();
            }
        }
    }

    // Vật cản (Obstacle / Barricade) là vật cứng: không bỏ va chạm nữa.
    // Việc chặn xe do scooter.cs xử lý.
}
