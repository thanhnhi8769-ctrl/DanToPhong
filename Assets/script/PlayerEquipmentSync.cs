using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerEquipmentSync : MonoBehaviourPunCallbacks
{
    [Header("(Không còn dùng - có thể để trống)")]
    public GameObject[] scooterModels;

    private scooter xe;

    private void Start()
    {
        DongBoThuocTinhCuaMinh();
        ApDungXeDaChon();
    }

    public override void OnEnable()
    {
        base.OnEnable();

        // Lần đầu do Start xử lý; chỉ áp dụng lại khi object được bật lại
        if (xe != null) ApDungXeDaChon();
    }

    public override void OnJoinedRoom()
    {
        DongBoThuocTinhCuaMinh();
        ApDungXeDaChon();
    }

    // Chỉ gửi lên mạng khi giá trị thật sự khác (trước đây gửi lặp ở Start + OnJoinedRoom + GameNetworkManager)
    private void DongBoThuocTinhCuaMinh()
    {
        if (photonView == null || !photonView.IsMine || !PhotonNetwork.InRoom) return;

        int idx = VehicleCatalog.GetSelectedIndex();

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Scooter", out object cu) &&
            cu != null && System.Convert.ToInt32(cu) == idx)
            return;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "Scooter", idx } });
    }

    // Mỗi xe lấy màu theo InstantiationData của CHÍNH xe đó.
    // Thuộc tính của người chơi chỉ là phương án dự phòng (trước đây nó ghi đè nên dễ sai màu).
    public void ApDungXeDaChon()
    {
        if (xe == null) xe = GetComponentInParent<scooter>();
        if (xe == null) return;

        int indexXe = -1;

        object[] duLieuSpawn = photonView != null ? photonView.InstantiationData : null;
        if (duLieuSpawn != null && duLieuSpawn.Length > 0 && duLieuSpawn[0] != null)
            indexXe = System.Convert.ToInt32(duLieuSpawn[0]);

        if (indexXe < 0)
        {
            if (!PhotonNetwork.IsConnected || photonView == null || photonView.IsMine)
            {
                indexXe = VehicleCatalog.GetSelectedIndex();
            }
            else if (photonView.Owner != null &&
                     photonView.Owner.CustomProperties != null &&
                     photonView.Owner.CustomProperties.TryGetValue("Scooter", out object val) && val != null)
            {
                indexXe = System.Convert.ToInt32(val);
            }
        }

        if (indexXe < 0) return;

        // scooter.ApDungMauXe tự bỏ qua nếu xe đã đúng mẫu này
        xe.ApDungMauXe(indexXe);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        if (photonView != null && photonView.Owner == targetPlayer && changedProps.ContainsKey("Scooter"))
            ApDungXeDaChon();
    }
}