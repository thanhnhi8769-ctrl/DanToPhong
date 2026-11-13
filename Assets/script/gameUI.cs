using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

// Đồng hồ đã do GameManager cập nhật, script này chỉ còn lo các nút.
public class GameUi : MonoBehaviour
{
    // Nút Chơi Lại (chỉ dùng khi chơi offline; online nên về Lobby)
    public void ChoiLai()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("game");
    }

    // Nút Về Menu
    public void VeMenu()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
        {
            // GameManager.OnLeftRoom sẽ tự load scene menu
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("menu");
        }
    }
}