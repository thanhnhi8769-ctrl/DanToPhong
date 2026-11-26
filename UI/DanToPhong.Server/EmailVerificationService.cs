using System.Net;
using System.Net.Mail;

namespace DanToPhong.Server;

internal static class EmailVerificationService
{
    public static async Task SendCodeAsync(string email, string code, bool passwordReset)
    {
        string user = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_USER") ?? "";
        string password = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_PASSWORD") ?? "";
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP credentials are not configured.");

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user.Trim(), password.Replace(" ", "").Trim())
        };
        using var mail = new MailMessage
        {
            From = new MailAddress(user.Trim(), "Scooter Racing"),
            Subject = passwordReset ? "Scooter Racing - Password Reset Code" : "Scooter Racing - Registration Code",
            Body = passwordReset
                ? $"Your password reset code is {code}. It expires in 10 minutes."
                : $"Your registration code is {code}. It expires in 10 minutes.",
            IsBodyHtml = false
        };
        mail.To.Add(email.Trim());
        try { await client.SendMailAsync(mail).WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false); }
        catch (TimeoutException) { client.SendAsyncCancel(); throw; }
    }
}
