using UnityEngine;

/// Tự chạy khi mở game, KHÔNG cần kéo vào scene nào.
/// Giảm tải CPU/GPU khi mở nhiều client để kiểm thử trên cùng một máy.
public static class PerformanceBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // Giới hạn 60 FPS: không có giới hạn thì mỗi bản game chạy 200-300 FPS, ăn hết CPU/GPU
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Cho phép cửa sổ không được chọn vẫn chạy (cần khi test 2 bản trên 1 máy)
        Application.runInBackground = true;

        // Bản build thường: tắt Debug.Log vì log cũng tốn CPU. Editor / Development Build vẫn giữ log để debug.
        Debug.unityLogger.logEnabled = Debug.isDebugBuild;

    }
}