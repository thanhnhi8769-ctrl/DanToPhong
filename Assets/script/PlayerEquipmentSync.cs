using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerEquipmentSync : MonoBehaviourPunCallbacks
{
    [Header("Model theo thứ tự Shop")]
    public GameObject[] scooterModels;

    private void Start()
    {
        ApDungXeDaChon();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        ApDungXeDaChon();
    }

    public void ApDungXeDaChon()
    {
        if (scooterModels == null || scooterModels.Length == 0) return;

        int indexXe = 0;

        // Nếu là xe của mình -> lấy trực tiếp từ PlayerInventory
        if (!PhotonNetwork.IsConnected || photonView == null || photonView.IsMine)
        {
            indexXe = PlayerInventory.GetEquippedIndex("Scooter");
        }
        // Nếu là xe của người chơi khác trong phòng -> lấy từ CustomProperties của họ
        else if (photonView.Owner != null &&
                 photonView.Owner.CustomProperties.TryGetValue("Scooter", out object val))
        {
            indexXe = System.Convert.ToInt32(val);
        }

        indexXe = Mathf.Clamp(indexXe, 0, scooterModels.Length - 1);

        for (int i = 0; i < scooterModels.Length; i++)
        {
            if (scooterModels[i] != null)
                scooterModels[i].SetActive(i == indexXe);
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        if (photonView != null && photonView.Owner == targetPlayer && changedProps.ContainsKey("Scooter"))
        {
            ApDungXeDaChon();
        }
    }
}