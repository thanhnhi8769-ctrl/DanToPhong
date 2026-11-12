using UnityEngine;
using Photon.Pun;

public class GameNetworkManager : MonoBehaviour
{
    // Danh sách chứa 4 vị trí xuất phát
    public Transform[] spawnPoints;

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && spawnPoints.Length > 0)
        {
            // Lấy số thứ tự của người chơi trong phòng (người 1 = 0, người 2 = 1, người 3 = 2, người 4 = 3)
            int playerIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;

            Transform chosenSpawn = spawnPoints[playerIndex];

            // Sinh ra chiếc xe tại đúng vị trí xuất phát của người đó
            PhotonNetwork.Instantiate("scooter", chosenSpawn.position, chosenSpawn.rotation);
        }
    }
}