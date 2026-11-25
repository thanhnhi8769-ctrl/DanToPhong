using UnityEngine;
using TMPro;

public class PasswordMaskStyle : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void Awake()
    {
        if (passwordInput == null)
        {
            Debug.LogWarning("Chua gan InputPassword!");
            return;
        }

        // Dấu chấm tròn dùng để che mật khẩu.
        passwordInput.asteriskChar = '•';

        // Cập nhật giao diện.
        passwordInput.ForceLabelUpdate();
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
