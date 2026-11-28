using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Menu = "menu";
    public const string Shop = "ShopScene";
    public const string Select = "SelectScene";
    public const string Lobby = "LobbyScene";
    public const string Game1 = "game1";
    public const string Game2 = "game2";
    public const string Game3 = "game3";
    public const string DefaultMap = Game1;

    public static readonly string[] Maps = { Game1, Game2, Game3 };

    public static bool IsRaceMap(string sceneName)
    {
        return string.Equals(sceneName, Game1, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, Game2, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, Game3, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeMap(string sceneName)
    {
        if (string.Equals(sceneName, Game1, StringComparison.OrdinalIgnoreCase)) return Game1;
        if (string.Equals(sceneName, Game2, StringComparison.OrdinalIgnoreCase)) return Game2;
        if (string.Equals(sceneName, Game3, StringComparison.OrdinalIgnoreCase)) return Game3;
        if (string.Equals(sceneName, "game 2", StringComparison.OrdinalIgnoreCase)) return Game2;
        if (string.Equals(sceneName, "game 3", StringComparison.OrdinalIgnoreCase)) return Game3;
        return DefaultMap;
    }
}

// Đặt trong scene "Boot" (trống) và để ở INDEX 0 của Build Settings.
public class Boot : MonoBehaviour
{
    public string sceneMenu = SceneNames.Menu;
    public string sceneSolo = SceneNames.Select;
    public string sceneNhieuNguoi = SceneNames.Lobby;
    public string sceneShop = SceneNames.Shop;

    [Tooltip("Bật: chạy từ launcher vẫn vào scene menu (có thẻ thông tin người chơi) thay vì vào thẳng Select/Lobby.")]
    public bool vaoMenuKhiCoLauncher = false;

    private async void Start()
    {
        Time.timeScale = 1f;

        // Chạy thẳng trong Editor (không qua launcher): vào menu như cũ
        if (!LauncherArgs.HasLauncher)
        {
            SceneManager.LoadScene(sceneMenu);
            return;
        }

        bool ok = await ServerClient.ConnectAsync();
        if (!ok)
        {
            Debug.LogError("[Boot] Không kết nối / xác thực được server. Thoát game.");
            Application.Quit();
            return;
        }

        // Lấy ID + tên để hiển thị trong phòng chờ: "username #ID"
        Resp info = await ServerClient.CallAsync(new Req { command = "LAYTHONGTIN" });
        if (info != null && info.success && info.user != null)
        {
            LauncherArgs.RacerId = info.user.racerId ?? "";
            LauncherArgs.FullName = info.user.fullName ?? "";
        }
        Debug.Log($"[Boot] user={LauncherArgs.User} racerId={LauncherArgs.RacerId} coins={ServerClient.Cache.coins} mode={LauncherArgs.Mode}");

        // Dịch vụ chạy xuyên scene: báo phòng lên server + nhận lời mời
        GameObject svc = new GameObject("LauncherServices");
        DontDestroyOnLoad(svc);
        svc.AddComponent<RoomReporter>();
        svc.AddComponent<InviteListener>();
        svc.AddComponent<ServerHeartbeat>();

        string dich;
        if (vaoMenuKhiCoLauncher && LauncherArgs.Mode == "menu")
        {
            SceneManager.LoadScene(sceneMenu);
            return;
        }

        switch (LauncherArgs.Mode)
        {
            case "single": dich = sceneSolo; break;
            case "shop": dich = sceneShop; break;
            case "menu": dich = sceneMenu; break;
            default: dich = sceneNhieuNguoi; break;
        }

        if (!Application.CanStreamedLevelBeLoaded(dich))
        {
            Debug.LogError($"[Boot] Không thấy scene '{dich}' trong Build Settings.");
            Application.Quit();
            return;
        }

        SceneManager.LoadScene(dich);
    }

    private void OnApplicationQuit()
    {
        ServerClient.Close();
    }
}