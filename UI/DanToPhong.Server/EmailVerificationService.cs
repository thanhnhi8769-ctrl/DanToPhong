using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DanToPhong.Server;

internal static class EmailVerificationService
{
    public static async Task SendCodeAsync(string email, string code, bool passwordReset)
    {
        string user = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_USER") ?? "";
        string password = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_PASSWORD") ?? "";
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP credentials are not configured.");

        string host = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_HOST") ?? "smtp.gmail.com";
        string rawPort = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_PORT") ?? "465";
        if (!int.TryParse(rawPort, out int port) || port is not (465 or 587))
            throw new InvalidOperationException("DANTOPHONG_SMTP_PORT must be 465 (implicit TLS) or 587 (STARTTLS).");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Scooter Racing", user.Trim()));
        message.To.Add(MailboxAddress.Parse(email.Trim()));
        message.Subject = passwordReset ? "Scooter Racing - Password Reset Code" : "Scooter Racing - Registration Code";
        message.Body = new TextPart("plain")
        {
            Text = passwordReset
                ? $"Your password reset code is {code}. It expires in 10 minutes."
                : $"Your registration code is {code}. It expires in 10 minutes."
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var client = new SmtpClient
        {
            Timeout = 15000
        };
        SecureSocketOptions socketOptions = port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(host, port, socketOptions, timeout.Token).ConfigureAwait(false);
        await client.AuthenticateAsync(user.Trim(), password.Replace(" ", "").Trim(), timeout.Token).ConfigureAwait(false);
        await client.SendAsync(FormatOptions.Default, new SmtpSendRequest(message), timeout.Token).ConfigureAwait(false);
        await client.DisconnectAsync(true, timeout.Token).ConfigureAwait(false);
    }
}
