using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ForgotPasswordValidation : MonoBehaviour
{
    [Header("Email Input")]
    [SerializeField] private TMP_InputField inputForgotEmail;

    [Header("Error Message")]
    [SerializeField] private TMP_Text textForgotEmailError;

    private bool hasSubmitted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (inputForgotEmail == null ||
        textForgotEmailError == null)
        {
            Debug.LogError(
                "ForgotPassword: Chua gan du tham chieu!"
            );
            enabled = false;
            return;
        }

        textForgotEmailError.text = "";

        inputForgotEmail.onValueChanged.AddListener(
            OnEmailChanged
        );

    }
    private void OnDestroy()
    {
        if (inputForgotEmail != null)
            inputForgotEmail.onValueChanged.RemoveListener(
                OnEmailChanged
            );
    }

    private void OnEmailChanged(string value)
    {
        if (hasSubmitted)
            ValidateEmail();
    }

    public void OnSendCodeClicked()
    {
        hasSubmitted = true;

        if (!ValidateEmail())
        {
            Debug.LogWarning(
                "ForgotPassword: Email khong hop le."
            );
            return;
        }

        Debug.Log(
            "ForgotPassword: Email hop le, san sang goi API."
        );

        // Chua gui OTP tai giai doan nay.
        // Backend API se xu ly gui ma xac nhan sau.
    }

    private bool ValidateEmail()
    {
        string email = inputForgotEmail.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Vui lòng nhập Email.");
            return false;
        }

        if (!Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ShowError("Email không đúng định dạng.");
            return false;
        }

        textForgotEmailError.text = "";
        return true;
    }

    private void ShowError(string message)
    {
        textForgotEmailError.text = message;
        textForgotEmailError.color =
            new Color32(255, 119, 119, 255);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
