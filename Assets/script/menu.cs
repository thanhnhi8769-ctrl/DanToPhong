using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Tên Scene")]
    public string tenSceneChon = "SelectScene";
    public string tenSceneShop = "ShopScene";

    [Header("Xác nhận thoát (tùy chọn)")]
    [Tooltip("Gán panel xác nhận nếu muốn ESC hỏi trước khi thoát. Để trống thì ESC thoát luôn.")]
    public GameObject panelXacNhanThoat;

    private bool dangChuyenScene = false;

    private void Start()
    {
        Time.timeScale = 1f;
        if (panelXacNhanThoat != null) panelXacNhanThoat.SetActive(false);
    }

    private void Update()
    {
        if (!NhanEsc()) return;

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

    private static bool NhanEsc()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // ------------------------------------------------------------------
    // GẮN VÀO BUTTON
    // ------------------------------------------------------------------
    public void BatTatNhac() { MenuLobbyMusic.BatTat(); }

    public void ChoiGame() { ChuyenScene(tenSceneChon); }
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