using UnityEngine;

// Bật VSync cho mọi scene ngay khi game khởi động: hết hiện tượng xé ngang màn hình (screen tearing).
// Khi VSync bật, Unity tự khóa FPS theo tần số quét của màn hình nên targetFrameRate không còn tác dụng.
public static class DisplaySettingsBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
