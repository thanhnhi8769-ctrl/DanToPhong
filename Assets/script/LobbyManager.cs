using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Giao diện sảnh chờ")]
    public TMP_Text statusText;
    public TMP_InputField roomInput;
    public GameObject createButton; // Kéo nút CreateRoom vào đây
    public GameObject joinButton;   // Kéo nút JoinRoom vào đây
    public GameObject startButton;  // Kéo nút StartButton vào đây

    private bool dangXuLy = false;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (startButton != null) startButton.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        statusText.text = "Đang kết nối tới Server...";
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Đã kết nối! Nhập tên phòng và chọn Tạo hoặc Vào.";
        }
    }

    void Update()
    {
        // Nhấn phím ESC hoạt động giống hệt bấm nút EXIT
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BamNutExit();
        }
    }

    // Gắn hàm này vào sự kiện On Click () của nút EXIT
    public void BamNutExit()
    {
        if (dangXuLy) return;

        if (PhotonNetwork.InRoom)
        {
            dangXuLy = true;
            statusText.text = "Đang rời phòng...";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            SceneManager.LoadScene(0);
        }
    }

    public override void OnLeftRoom()
    {
        dangXuLy = false;
        StopAllCoroutines();

        if (roomInput != null) roomInput.gameObject.SetActive(true);
        if (createButton != null) createButton.SetActive(true);
        if (joinButton != null) joinButton.SetActive(true);
        if (startButton != null) startButton.SetActive(false);

        statusText.text = "Đã rời phòng. Nhập tên phòng và chọn Tạo hoặc Vào.";
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Đã kết nối! Nhập tên phòng và chọn Tạo hoặc Vào.";
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (string.IsNullOrEmpty(roomInput.text))
        {
            statusText.text = "Vui lòng nhập tên phòng trước!";
            return;
        }

        dangXuLy = true;
        statusText.text = "Đang tạo phòng...";
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(roomInput.text, roomOptions);
    }

    public void JoinRoom()
    {
        if (dangXuLy || PhotonNetwork.InRoom) return;

        if (string.IsNullOrEmpty(roomInput.text))
        {
            statusText.text = "Vui lòng nhập tên phòng muốn vào!";
            return;
        }

        dangXuLy = true;
        statusText.text = "Đang tìm và vào phòng...";
        PhotonNetwork.JoinRoom(roomInput.text);
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(false);
            statusText.text = "Đang tải đường đua...";
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel("game");
        }
    }

    public override void OnJoinedRoom()
    {
        dangXuLy = false;

        roomInput.gameObject.SetActive(false);
        createButton.SetActive(false);
        joinButton.SetActive(false);

        CapNhatTrangThaiPhong();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string ten = string.IsNullOrEmpty(newPlayer.NickName)
            ? $"Người chơi #{newPlayer.ActorNumber}"
            : newPlayer.NickName;

        StopAllCoroutines();
        StartCoroutine(HienThongBaoSuKienPhong($"<color=#00FF00>{ten} đã vào phòng!</color>"));
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        string ten = string.IsNullOrEmpty(otherPlayer.NickName)
            ? $"Người chơi #{otherPlayer.ActorNumber}"
            : otherPlayer.NickName;

        StopAllCoroutines();
        StartCoroutine(HienThongBaoSuKienPhong($"<color=#FFFF00>{ten} đã thoát khỏi phòng!</color>"));
    }

    private IEnumerator HienThongBaoSuKienPhong(string thongBao)
    {
        int soNguoi = PhotonNetwork.CurrentRoom.PlayerCount;
        int toiDa = PhotonNetwork.CurrentRoom.MaxPlayers;

        if (PhotonNetwork.IsMasterClient)
        {
            if (startButton != null) startButton.SetActive(true);
            statusText.text = $"{thongBao}\nPhòng: {PhotonNetwork.CurrentRoom.Name} ({soNguoi}/{toiDa} người) - Bấm START GAME khi sẵn sàng!";
        }
        else
        {
            if (startButton != null) startButton.SetActive(false);
            statusText.text = $"{thongBao}\nPhòng: {PhotonNetwork.CurrentRoom.Name} ({soNguoi}/{toiDa} người) - Đang chờ Chủ Phòng bắt đầu...";
        }

        yield return new WaitForSeconds(3.5f);

        if (PhotonNetwork.InRoom)
        {
            CapNhatTrangThaiPhong();
        }
    }

    private void CapNhatTrangThaiPhong()
    {
        int soNguoi = PhotonNetwork.CurrentRoom.PlayerCount;
        int toiDa = PhotonNetwork.CurrentRoom.MaxPlayers;

        if (PhotonNetwork.IsMasterClient)
        {
            if (startButton != null) startButton.SetActive(true);
            statusText.text = $"Phòng: {PhotonNetwork.CurrentRoom.Name} ({soNguoi}/{toiDa} người)\nBạn là Chủ Phòng. Bấm START GAME khi sẵn sàng!";
        }
        else
        {
            if (startButton != null) startButton.SetActive(false);
            statusText.text = $"Phòng: {PhotonNetwork.CurrentRoom.Name} ({soNguoi}/{toiDa} người)\nĐang chờ Chủ Phòng bắt đầu...";
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;
        statusText.text = "Tên phòng đã tồn tại hoặc lỗi tạo phòng!";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        dangXuLy = false;
        statusText.text = "Không tìm thấy phòng hoặc phòng đã đầy!";
    }
}