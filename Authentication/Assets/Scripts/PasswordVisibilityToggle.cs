using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordVisibilityToggle : MonoBehaviour
{
    [Header("Password Input")]
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Eye Icons")]
    [SerializeField] private Image eyeImage;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    private bool isPasswordVisible = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Mặc định: ẩn mật khẩu
        isPasswordVisible = false;
        UpdatePasswordUI();

    }
    public void TogglePasswordVisibility()
    {
        // Đảo trạng thái ẩn/hiện
        isPasswordVisible = !isPasswordVisible;

        UpdatePasswordUI();
    }
    private void UpdatePasswordUI()
    {
        if (passwordInput == null)
        {
            Debug.LogWarning("Chua gan InputPassword!");
            return;
        }

        // Chuyển chế độ hiển thị
        passwordInput.contentType = isPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        // Cập nhật nội dung hiển thị ngay
        passwordInput.ForceLabelUpdate();

        // Đổi hình con mắt
        if (eyeImage != null)
        {
            eyeImage.sprite = isPasswordVisible
                ? eyeOpenSprite
                : eyeClosedSprite;
        }

        Debug.Log(
            isPasswordVisible
                ? "Password Visible"
                : "Password Hidden"
        );
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
