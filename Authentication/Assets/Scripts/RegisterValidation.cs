using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class RegisterValidation : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputEmail;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private TMP_InputField inputConfirmPassword;

    [Header("Terms")]
    [SerializeField] private Toggle toggleTerms;

    [Header("Error Messages")]
    [SerializeField] private TMP_Text textUsernameError;
    [SerializeField] private TMP_Text textEmailError;
    [SerializeField] private TMP_Text textPasswordError;
    [SerializeField] private TMP_Text textConfirmError;
    [SerializeField] private TMP_Text textTermsError;

    private bool hasSubmitted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!HasAllReferences())
        {
            Debug.LogError("RegisterValidation: Thieu tham chieu Inspector!");
            enabled = false;
            return;
        }
        ClearAllErrors();

        // Khi người chơi sửa dữ liệu, kiểm tra lại.
        inputUsername.onValueChanged.AddListener(OnInputChanged);
        inputEmail.onValueChanged.AddListener(OnInputChanged);
        inputPassword.onValueChanged.AddListener(OnInputChanged);
        inputConfirmPassword.onValueChanged.AddListener(OnInputChanged);
        toggleTerms.onValueChanged.AddListener(OnTermsChanged);
    }

    private bool HasAllReferences()
    {
        return inputUsername != null &&
               inputEmail != null &&
               inputPassword != null &&
               inputConfirmPassword != null &&
               toggleTerms != null &&
               textUsernameError != null &&
               textEmailError != null &&
               textPasswordError != null &&
               textConfirmError != null &&
               textTermsError != null;
    }


    private void OnDestroy()
    {
        if (inputUsername != null)
            inputUsername.onValueChanged.RemoveListener(OnInputChanged);

        if (inputEmail != null)
            inputEmail.onValueChanged.RemoveListener(OnInputChanged);

        if (inputPassword != null)
            inputPassword.onValueChanged.RemoveListener(OnInputChanged);

        if (inputConfirmPassword != null)
            inputConfirmPassword.onValueChanged.RemoveListener(OnInputChanged);

        if (toggleTerms != null)
            toggleTerms.onValueChanged.RemoveListener(OnTermsChanged);
    }
    private void OnInputChanged(string value)
    {
        if (hasSubmitted)
            ValidateAll();
    }

    private void OnTermsChanged(bool value)
    {
        if (hasSubmitted)
            ValidateAll();
    }

    public void OnRegisterClicked()
    {
        hasSubmitted = true;

        bool isValid = ValidateAll();

        if (!isValid)
        {
            Debug.LogWarning("Register: Du lieu khong hop le.");
            return;
        }

        Debug.Log("Register: Du lieu hop le, san sang gui API.");

        // Chua tao tai khoan tai buoc nay.
        // Sau nay se goi Backend API tai day.
    }

    private bool ValidateAll()
    {
        ClearAllErrors();

        bool isValid = true;

        // 1. Username
        string username = inputUsername.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError(textUsernameError,
                "Vui lòng nhập tên người chơi.");
            isValid = false;
        }
        else if (username.Length < 3 || username.Length > 30)
        {
            ShowError(textUsernameError,
                "Tên người chơi phải từ 3-30 ký tự.");
            isValid = false;
        }

        // 2. Email
        string email = inputEmail.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(textEmailError,
                "Vui lòng nhập Email.");
            isValid = false;
        }
        else if (!IsValidEmail(email))
        {
            ShowError(textEmailError,
                "Email không đúng định dạng.");
            isValid = false;
        }

        // 3. Password
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(password))
        {
            ShowError(textPasswordError,
                "Vui lòng nhập mật khẩu.");
            isValid = false;
        }
        else if (password.Length < 8 ||
                 !Regex.IsMatch(password, @"\p{L}") ||
                 !Regex.IsMatch(password, @"[0-9]"))
        {
            ShowError(textPasswordError,
                "Mật khẩu cần ít nhất 8 ký tự, có chữ và số.");
            isValid = false;
        }

        // 4. Confirm Password
        string confirmPassword = inputConfirmPassword.text;

        if (string.IsNullOrEmpty(confirmPassword))
        {
            ShowError(textConfirmError,
                "Vui lòng xác nhận mật khẩu.");
            isValid = false;
        }
        else if (confirmPassword != password)
        {
            ShowError(textConfirmError,
                "Mật khẩu xác nhận không khớp.");
            isValid = false;
        }

        // 5. Terms
        if (!toggleTerms.isOn)
        {
            ShowError(textTermsError,
                "Vui lòng đồng ý với điều khoản sử dụng.");
            isValid = false;
        }

        return isValid;
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private void ShowError(TMP_Text errorText, string message)
    {
        errorText.text = message;
        errorText.color = new Color32(255, 119, 119, 255);
    }

    private void ClearAllErrors()
    {
        if (textUsernameError != null)
            textUsernameError.text = "";

        if (textEmailError != null)
            textEmailError.text = "";

        if (textPasswordError != null)
            textPasswordError.text = "";

        if (textConfirmError != null)
            textConfirmError.text = "";

        if (textTermsError != null)
            textTermsError.text = "";
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
