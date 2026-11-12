using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuLobbyMusic : MonoBehaviour
{
    public AudioClip nhacMenuLobby;
    [Range(0f, 1f)] public float amLuong = 0.45f;

    private static MenuLobbyMusic instance;
    private AudioSource audioSource;

    private void Awake()
    {
        // Giữ 1 trình phát nhạc duy nhất xuyên suốt giữa menu và LobbyScene
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = nhacMenuLobby;
        audioSource.loop = true;
        audioSource.volume = amLuong;
        audioSource.playOnAwake = false;

        if (nhacMenuLobby != null) audioSource.Play();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += KhiChuyenScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= KhiChuyenScene;
    }

    private void KhiChuyenScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "game")
        {
            // Vào đường đua thì tạm dừng nhạc sảnh để GameManager phát nhạc đua xe
            if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
        }
        else
        {
            // Ở menu hoặc LobbyScene thì phát nhạc sảnh liên tục
            if (audioSource != null && !audioSource.isPlaying && nhacMenuLobby != null)
            {
                audioSource.Play();
            }
        }
    }
}