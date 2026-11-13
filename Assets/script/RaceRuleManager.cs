using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RaceRuleManager : MonoBehaviourPunCallbacks
{
    public static RaceRuleManager Instance;

    [Header("Cài đặt phần thưởng & Scene")]
    public int thuongNguoiThang = 50;
    public string menuSceneName = "menu";

    [Header("Giao diện Khi Bị Loại (Ở lại xem hoặc Thoát)")]
    public GameObject bangBiLoaiPanel;   // Panel hiện lên khi bạn chết (chứa 2 nút: Ở lại xem / Thoát)
    public TMP_Text thongBaoBiLoaiText;

    [Header("Giao diện Kết Quả Cuối Trận")]
    public GameObject bangKetQuaPanel;   // Panel tổng kết khi trận đấu kết thúc
    public TMP_Text ketQuaText;

    private HashSet<int> danhSachNguoiChet = new HashSet<int>();
    private bool daKetThucTran = false;
    private bool toiDaChet = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (bangBiLoaiPanel != null) bangBiLoaiPanel.SetActive(false);
        if (bangKetQuaPanel != null) bangKetQuaPanel.SetActive(false);
        danhSachNguoiChet.Clear();
        daKetThucTran = false;
        toiDaChet = false;
    }

    // =========================================================================
    // 1. GỌI KHI CHẠM VẠCH ĐÍCH
    // =========================================================================
    public void BaoNguoiChoiVeDich(PhotonView playerView)
    {
        if (daKetThucTran || playerView == null || !playerView.IsMine) return;

        // Nếu chơi Solo hoặc Offline
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            daKetThucTran = true;
            PlayerInventory.AddCoins(thuongNguoiThang);

            if (bangKetQuaPanel != null) bangKetQuaPanel.SetActive(true);
            if (ketQuaText != null)
            {
                ketQuaText.text = $"<color=#00FF00>VICTORY!</color>\n(FIRST TO FINISH!)\n+{thuongNguoiThang} COINS";
            }
            return;
        }

        int winnerActor = PhotonNetwork.LocalPlayer.ActorNumber;
        string winnerName = string.IsNullOrEmpty(PhotonNetwork.NickName)
            ? $"Player {winnerActor}"
            : PhotonNetwork.NickName;

        photonView.RPC(nameof(RPC_ThongBaoNguoiThang), RpcTarget.All, winnerActor, winnerName, "FIRST TO FINISH!");
    }

    // =========================================================================
    // 2. GỌI KHI XE CỦA NGƯỜI CHƠI BỊ CHẾT / RƠI VỰC
    // =========================================================================
    public void BaoNguoiChoiBiChet(PhotonView playerView)
    {
        if (daKetThucTran || playerView == null || !playerView.IsMine || toiDaChet) return;

        toiDaChet = true;
        int deadActor = PhotonNetwork.LocalPlayer.ActorNumber;

        // Khóa điều khiển và ẩn xe người vừa chết (giữ nguyên Camera để xem)
        VoHieuHoaXeKhiChet(playerView.gameObject);

        // Chế độ Solo (1 người): Chết là báo thua luôn
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            daKetThucTran = true;
            if (bangKetQuaPanel != null) bangKetQuaPanel.SetActive(true);
            if (ketQuaText != null)
            {
                ketQuaText.text = "<color=#FF5555>DEFEAT!</color>\nYou crashed!\n+0 COINS";
            }
            return;
        }

        // Chế độ Multiplayer (>= 2 người): Cập nhật cho cả phòng và hỏi Ở lại xem / Thoát
        photonView.RPC(nameof(RPC_DanhDauNguoiChet), RpcTarget.All, deadActor);

        if (!daKetThucTran && bangBiLoaiPanel != null)
        {
            bangBiLoaiPanel.SetActive(true);
            if (thongBaoBiLoaiText != null)
            {
                thongBaoBiLoaiText.text = "YOU ARE ELIMINATED!\nStay to watch or leave match?";
            }
        }
    }

    private void VoHieuHoaXeKhiChet(GameObject xeObj)
    {
        foreach (Rigidbody rb in xeObj.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
        }

        foreach (Renderer r in xeObj.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }

        foreach (Collider col in xeObj.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
    }

    // =========================================================================
    // CÁC NÚT BẤM CHO NGƯỜI ĐÃ CHẾT & KẾT THÚC TRẬN
    // =========================================================================

    // Gắn vào nút "STAY & WATCH"
    public void NutOLaiXem()
    {
        if (bangBiLoaiPanel != null)
        {
            bangBiLoaiPanel.SetActive(false);
        }
    }

    // Gắn vào nút "LEAVE MATCH" và nút "BACK TO MENU" trên bảng kết quả
    public void RoiPhongVeMenu()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else SceneManager.LoadScene(menuSceneName);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    // =========================================================================
    // LOGIC TRỌNG TÀI XÉT THẮNG / THUA QUA MẠNG
    // =========================================================================

    [PunRPC]
    private void RPC_DanhDauNguoiChet(int deadActor)
    {
        if (daKetThucTran) return;

        danhSachNguoiChet.Add(deadActor);

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            KiemTraNguoiSongSotCuoiCung();
        }
    }

    private void KiemTraNguoiSongSotCuoiCung()
    {
        if (daKetThucTran || !PhotonNetwork.InRoom) return;

        List<Player> nguoiConSong = new List<Player>();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!danhSachNguoiChet.Contains(p.ActorNumber))
            {
                nguoiConSong.Add(p);
            }
        }

        if (nguoiConSong.Count == 1)
        {
            Player lastSurvivor = nguoiConSong[0];
            string winnerName = string.IsNullOrEmpty(lastSurvivor.NickName)
                ? $"Player {lastSurvivor.ActorNumber}"
                : lastSurvivor.NickName;

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_ThongBaoNguoiThang), RpcTarget.All, lastSurvivor.ActorNumber, winnerName, "LAST SURVIVOR!");
            }
        }
        else if (nguoiConSong.Count == 0 && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ThongBaoHoa), RpcTarget.All);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!daKetThucTran && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount >= 1)
        {
            KiemTraNguoiSongSotCuoiCung();
        }
    }

    [PunRPC]
    private void RPC_ThongBaoNguoiThang(int winnerActor, string winnerName, string lyDoThang)
    {
        if (daKetThucTran) return;
        daKetThucTran = true;

        if (bangBiLoaiPanel != null) bangBiLoaiPanel.SetActive(false);

        bool toiThang = (PhotonNetwork.LocalPlayer.ActorNumber == winnerActor);

        if (toiThang)
        {
            PlayerInventory.AddCoins(thuongNguoiThang);
        }

        if (bangKetQuaPanel != null) bangKetQuaPanel.SetActive(true);
        if (ketQuaText != null)
        {
            ketQuaText.text = toiThang
                ? $"<color=#00FF00>VICTORY!</color>\n({lyDoThang})\n+{thuongNguoiThang} COINS"
                : $"<color=#FF5555>DEFEAT!</color>\nWinner: {winnerName}\n({lyDoThang})";
        }
    }

    [PunRPC]
    private void RPC_ThongBaoHoa()
    {
        if (daKetThucTran) return;
        daKetThucTran = true;

        if (bangBiLoaiPanel != null) bangBiLoaiPanel.SetActive(false);
        if (bangKetQuaPanel != null) bangKetQuaPanel.SetActive(true);
        if (ketQuaText != null)
        {
            ketQuaText.text = "<color=#FFFF00>DRAW - ALL ELIMINATED!</color>";
        }
    }
}