using UnityEngine;
using UnityEngine.UI;

public class SoundToggleUI : MonoBehaviour
{
    [Header("Sound Icons")]
    [SerializeField] private Image soundIcon;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    private const string SoundKey = "SoundEnabled";

    private bool isSoundOn = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Lấy trạng thái âm thanh đã lưu.
        // Mặc định: 1 = bật âm thanh.
        isSoundOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;

        ApplySound();

    }

    public void ToggleSound()
    {
        // Chuyển ON thành OFF hoặc ngược lại.
        isSoundOn = !isSoundOn;

        // Lưu lựa chọn.
        PlayerPrefs.SetInt(SoundKey, isSoundOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplySound();
    }

    private void ApplySound()
    {
        // Bật hoặc tắt âm thanh toàn game.
        AudioListener.volume = isSoundOn ? 1f : 0f;

        // Đổi hình ảnh icon.
        if (soundIcon != null)
        {
            soundIcon.sprite = isSoundOn
                ? soundOnSprite
                : soundOffSprite;
        }

        Debug.Log(
            isSoundOn
                ? "Sound ON"
                : "Sound OFF"
        );
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
