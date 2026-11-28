using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room) && ServerClient.Ready)
            ServerClient.Fire(new Req { command = "ROOM_LEAVE" });
        SceneManager.LoadScene(SceneNames.Menu);
    }

    public void TiepTuc()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }
}