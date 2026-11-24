using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ResetPasswordValidation : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField inputOTP;
    [SerializeField] private TMP_InputField inputNewPassword;
    [SerializeField] private TMP_InputField inputConfirmPassword;

    [Header("Error Texts")]
    [SerializeField] private TMP_Text textOTPError;
    [SerializeField] private TMP_Text textNewPasswordError;
    [SerializeField] private TMP_Text textConfirmPasswordError;

    private bool hasSubmitted = false;

    private void Start()
    {
        if (inputOTP == null ||
            inputNewPassword == null ||
            inputConfirmPassword == null ||
            textOTPError == null ||
            textNewPasswordError == null ||
            textConfirmPasswordError == null)
        {
            Debug.LogError(
                "ResetPassword: Thieu tham chieu Inspector!"
            );
            enabled = false;
            return;
        }

        ClearErrors();

        inputOTP.onValueChanged.AddListener(OnChanged);
        inputNewPassword.onValueChanged.AddListener(OnChanged);
        inputConfirmPassword.onValueChanged.AddListener(OnChanged);
    }

    private void OnDestroy()
    {
        if (inputOTP != null)
            inputOTP.onValueChanged.RemoveListener(OnChanged);

        if (inputNewPassword != null)
            inputNewPassword.onValueChanged.RemoveListener(OnChanged);

        if (inputConfirmPassword != null)
            inputConfirmPassword.onValueChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(string value)
    {
        if (hasSubmitted)
            ValidateAll();
    }

    public void OnResetClicked()
    {
        hasSubmitted = true;

        if (!ValidateAll())
        {
            Debug.LogWarning("ResetPassword: Du lieu khong hop le.");
            return;
        }

        Debug.Log(
            "ResetPassword: Du lieu hop le, cho xac minh API."
        );

        // Chua doi mat khau tai Unity.
        // Backend se xac minh OTP va cap nhat mat khau.
    }

    private bool ValidateAll()
    {
        ClearErrors();
        bool valid = true;

        string otp = inputOTP.text.Trim();

        if (!Regex.IsMatch(otp, @"^[0-9]{6}$"))
        {
            ShowError(textOTPError,
                "Mã xác nhận phải gồm đúng 6 chữ số.");
            valid = false;
        }

        string password = inputNewPassword.text;

        if (password.Length < 8 ||
            !Regex.IsMatch(password, @"\p{L}") ||
            !Regex.IsMatch(password, @"[0-9]"))
        {
            ShowError(textNewPasswordError,
                "Mật khẩu cần ít nhất 8 ký tự, có chữ và số.");
            valid = false;
        }

        string confirm = inputConfirmPassword.text;

        if (string.IsNullOrEmpty(confirm))
        {
            ShowError(textConfirmPasswordError,
                "Vui lòng xác nhận mật khẩu mới.");
            valid = false;
        }
        else if (confirm != password)
        {
            ShowError(textConfirmPasswordError,
                "Mật khẩu xác nhận không khớp.");
            valid = false;
        }

        return valid;
    }

    private void ShowError(TMP_Text target, string message)
    {
        target.text = message;
        target.color = new Color32(255, 119, 119, 255);
    }

    private void ClearErrors()
    {
        if (textOTPError != null)
            textOTPError.text = "";

        if (textNewPasswordError != null)
            textNewPasswordError.text = "";

        if (textConfirmPasswordError != null)
            textConfirmPasswordError.text = "";
    }
}

