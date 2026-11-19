using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameUi : MonoBehaviour
{
    public void ChoiLai()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VeMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
            return;
        }

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("menu");
    }

    public void TiepTuc()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }
}