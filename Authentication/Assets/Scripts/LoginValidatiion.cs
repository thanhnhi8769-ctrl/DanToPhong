using UnityEngine;
using TMPro;

public class LoginValidatiion : MonoBehaviour
{
    [Header("Login Inputs")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputPassword;

    [Header("Error Messages")]
    [SerializeField] private TMP_Text textUsernameError;
    [SerializeField] private TMP_Text textPasswordError;

    private bool hasSubmitted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (inputUsername == null ||
           inputPassword == null ||
           textUsernameError == null ||
           textPasswordError == null)
        {
            Debug.LogError(
                "LoginValidation: Chua gan du tham chieu!"
            );

            enabled = false;
            return;
        }

        ClearAllErrors();

        inputUsername.onValueChanged.AddListener(OnInputChanged);
        inputPassword.onValueChanged.AddListener(OnInputChanged);

    }
    private void OnDestroy()
    {
        if (inputUsername != null)
        {
            inputUsername.onValueChanged.RemoveListener(OnInputChanged);
        }

        if (inputPassword != null)
        {
            inputPassword.onValueChanged.RemoveListener(OnInputChanged);
        }
    }
    private void OnInputChanged(string value)
    {
        if (hasSubmitted)
        {
            ValidateAll();
        }
    }

    public void OnLoginClicked()
    {
        hasSubmitted = true;

        bool isValid = ValidateAll();

        if (!isValid)
        {
            Debug.LogWarning("Login: Du lieu khong hop le.");
            return;
        }

        Debug.Log(
            "Login: Du lieu hop le, san sang gui API."
        );

        // Chua xac thuc tai khoan.
        // Sau nay se goi Backend API tai day.
    }

    private bool ValidateAll()
    {
        ClearAllErrors();

        bool isValid = true;

        // 1. Kiem tra Username/Email
        string username = inputUsername.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError(
                textUsernameError,
                "Vui lòng nhập tên đăng nhập hoặc Email."
            );

            isValid = false;
        }

        // 2. Kiem tra Password
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(password))
        {
            ShowError(
                textPasswordError,
                "Vui lòng nhập mật khẩu."
            );

            isValid = false;
        }

        return isValid;
    }

    private void ShowError(TMP_Text errorText, string message)
    {
        errorText.text = message;
        errorText.color = new Color32(255, 119, 119, 255);
    }

    private void ClearAllErrors()
    {
        if (textUsernameError != null)
        {
            textUsernameError.text = "";
        }

        if (textPasswordError != null)
        {
            textPasswordError.text = "";
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
