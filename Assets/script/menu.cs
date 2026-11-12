using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Hàm gọi khi bấm nút PLAY
    public void ChoiGame()
    {
        // Chuyển sang màn hình sảnh chờ để tạo/vào phòng trước khi đua
        SceneManager.LoadScene("LobbyScene");
    }

    // Hàm gọi khi bấm nút QUIT
    public void ThoatGame()
    {
        Debug.Log("Đã thoát game!");
        Application.Quit();
    }
}