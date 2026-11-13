using UnityEngine;
using Photon.Pun;

public class XuLyVaChamChoXe : MonoBehaviourPun
{
    private bool LaXeCuaMinh
    {
        get { return !PhotonNetwork.IsConnected || photonView == null || photonView.IsMine; }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. CheckPoint: xe nào ăn thì đồng xu biến mất trên mọi máy,
        //    nhưng chỉ chủ xe mới được cộng thời gian.
        if (other.CompareTag("CheckPoint"))
        {
            if (!other.gameObject.activeSelf) return;

            other.gameObject.SetActive(false);

            if (LaXeCuaMinh && GameManager.Instance != null)
                GameManager.Instance.QuaCheckPoint();

            return;
        }

        // 2. WinPoint: chỉ xe của mình
        if (other.CompareTag("WinPoint"))
        {
            if (LaXeCuaMinh && GameManager.Instance != null)
                GameManager.Instance.QuaWinPoint();
            return;
        }

        // 3. Rơi xuống nước (Trigger): chỉ tính cho xe của mình
        if (LaXeCuaMinh && (other.gameObject.name == "Sea_Plane" || other.CompareTag("Water")))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ChamNuocTruThoiGian();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!LaXeCuaMinh) return;

        // Rơi xuống nước (nếu Sea_Plane chưa tích Is Trigger)
        if (collision.gameObject.name == "Sea_Plane" || collision.gameObject.CompareTag("Water"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ChamNuocTruThoiGian();
            return;
        }

        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.name.Contains("Barricade"))
        {
            Debug.Log("Xe đã tông vào chướng ngại vật!");
        }
    }
}