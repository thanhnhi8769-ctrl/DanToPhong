using UnityEngine;

public class NhacNenXuyenSuot : MonoBehaviour
{
    private static NhacNenXuyenSuot instance;

    private void Awake()
    {
        // Giữ nguyên đối tượng nhạc này khi chuyển qua LobbyScene và game, tránh phát chồng 2 bài
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}