using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameNetworkManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && spawnPoints.Length > 0)
        {
            // Gửi chỉ số Xe, Mũ, Nhân vật của bản thân lên mạng để mọi người cùng thấy
            Hashtable trangBi = new Hashtable
            {
                { "Scooter", PlayerInventory.GetEquippedIndex("Scooter") },
                { "Helmet", PlayerInventory.GetEquippedIndex("Helmet") },
                { "Character", PlayerInventory.GetEquippedIndex("Character") }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(trangBi);

            int playerIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Transform chosenSpawn = spawnPoints[playerIndex];

            PhotonNetwork.Instantiate("scooter", chosenSpawn.position, chosenSpawn.rotation);
        }
    }
}