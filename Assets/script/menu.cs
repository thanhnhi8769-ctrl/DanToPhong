using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Tên Scene")]
    [Tooltip("Nút Play dẫn tới Lobby (chọn xe/map sẽ làm trong Lobby sau khi vào phòng)")]
    public string tenSceneLobby = "LobbyScene";
    public string tenSceneShop = "ShopScene";

    [Header("Xác nhận thoát (tùy chọn)")]
    [Tooltip("Gán panel xác nhận nếu muốn ESC hỏi trước khi thoát. Để trống thì ESC thoát luôn.")]
    public GameObject panelXacNhanThoat;

    [Header("Hiệu ứng nút")]
    public bool hieuUngNut = true;
    public Color mauVienNut = new Color(0.15f, 0.95f, 0.5f);

    private bool dangChuyenScene = false;

    private void Start()
    {
        Time.timeScale = 1f;
        if (panelXacNhanThoat != null) panelXacNhanThoat.SetActive(false);

        if (hieuUngNut) GanHieuUngChoMoiNut();
    }

    private void Update()
    {
        if (!InputEscapeUtility.TryConsume()) return;

        if (HelpSystem.DongNeuDangMo() || MenuLeaderboard.DongNeuDangMo()) return;

        if (panelXacNhanThoat != null)
        {
            // ESC bật/tắt panel xác nhận
            panelXacNhanThoat.SetActive(!panelXacNhanThoat.activeSelf);
        }
        else
        {
            ThoatGame();
        }
    }

    private void GanHieuUngChoMoiNut()
    {
        ButtonFx.MauChung = mauVienNut;

        // Include: lấy cả nút trong panel đang ẩn (panel xác nhận thoát...)
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.GetComponent<ButtonFx>() == null) b.gameObject.AddComponent<ButtonFx>();
        }
    }

    // ------------------------------------------------------------------
    // GẮN VÀO BUTTON
    // ------------------------------------------------------------------
    public void BatTatNhac() { MenuLobbyMusic.BatTat(); }

    /// Gắn vào nút "Play": Menu -> Lobby
    public void ChoiGame() { ChuyenScene(tenSceneLobby); }
    public void MoShop() { ChuyenScene(tenSceneShop); }

    /// Gắn vào nút "Không" / "Hủy" của panel xác nhận.
    public void HuyThoat()
    {
        if (panelXacNhanThoat != null) panelXacNhanThoat.SetActive(false);
    }

    /// Gắn vào nút "Thoát" và nút "Có" của panel xác nhận.
    public void ThoatGame()
    {
        if (dangChuyenScene) return;
        dangChuyenScene = true;

        Log("Đã thoát game!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------
    // HELPER
    // ------------------------------------------------------------------
    private void ChuyenScene(string tenScene)
    {
        if (dangChuyenScene) return;
        dangChuyenScene = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(tenScene);
    }

    [Conditional("UNITY_EDITOR")]
    private static void Log(string noiDung)
    {
        UnityEngine.Debug.Log(noiDung);
    }
}
