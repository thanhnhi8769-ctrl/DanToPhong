using UnityEngine;
using UnityEngine.SceneManagement;

// Gắn vào 1 GameObject trong scene "menu", rồi kéo nút:
//   PLAY -> OnClick -> MenuUI.Play
//   SHOP -> OnClick -> MenuUI.Shop
//   QUIT -> OnClick -> MenuUI.Quit
// Menu là trung tâm: Shop/Select/Lobby thoát đều quay về đây. Chỉ QUIT ở đây mới thoát game.
public class MenuUI : MonoBehaviour
{
    public string scenePlay = SceneNames.Lobby;
    public string sceneShop = SceneNames.Shop;

    public void Play() { SceneManager.LoadScene(scenePlay); }
    public void Shop() { SceneManager.LoadScene(sceneShop); }

    public void Quit() { DieuHuong.Thoat(); }
}