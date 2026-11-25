using UnityEngine;
using UnityEngine.SceneManagement;

public static class DieuHuong
{
    // Các scene (Shop, Select, Lobby...) "thoát" = quay về scene menu, KHÔNG thoát game.
    public static void VeMenu(string sceneMenu = "menu")
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneMenu);
    }

    // Chỉ nút QUIT ở menu mới gọi hàm này: thoát game, launcher hiện lại.
    public static void Thoat()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}