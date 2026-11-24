using UnityEngine;

public class AuthScreenManager : MonoBehaviour
{
    [Header("Authentication Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject forgotPasswordPanel;
    [SerializeField] private GameObject resetPasswordPanel;

    private void Start()
    {
        ShowLogin();
    }

    private void ShowPanel(GameObject target)
    {
        if (loginPanel == null ||
            registerPanel == null ||
            forgotPasswordPanel == null ||
            resetPasswordPanel == null)
        {
            Debug.LogError("AuthScreenManager: Thieu Panel!");
            return;
        }

        loginPanel.SetActive(target == loginPanel);
        registerPanel.SetActive(target == registerPanel);
        forgotPasswordPanel.SetActive(target == forgotPasswordPanel);
        resetPasswordPanel.SetActive(target == resetPasswordPanel);
    }

    public void ShowLogin()
    {
        ShowPanel(loginPanel);
        Debug.Log("Mo LoginPanel");
    }

    public void ShowRegister()
    {
        ShowPanel(registerPanel);
        Debug.Log("Mo RegisterPanel");
    }

    public void ShowForgotPassword()
    {
        ShowPanel(forgotPasswordPanel);
        Debug.Log("Mo ForgotPasswordPanel");
    }

    public void ShowResetPassword()
    {
        ShowPanel(resetPasswordPanel);
        Debug.Log("Mo ResetPasswordPanel");
    }
}

