using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DanToPhong.Server;

internal static class EmailVerificationService
{
    public static async Task SendCodeAsync(
        string email, string code, bool passwordReset, CancellationToken token)
    {
        string user = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_USER") ?? "";
        string password = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_PASSWORD") ?? "";
        string host = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_HOST") ?? "smtp.gmail.com";
        string rawPort = Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_PORT") ?? "465";
        bool smtp4dev = string.Equals(
            Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_MODE"), "smtp4dev", StringComparison.OrdinalIgnoreCase);
        if (!int.TryParse(rawPort, out int port))
            throw new InvalidOperationException("DANTOPHONG_SMTP_PORT must be a valid port number.");
        if (smtp4dev)
        {
            if (host is not ("localhost" or "127.0.0.1" or "::1") || port != 2525)
                throw new InvalidOperationException("smtp4dev mode is restricted to loopback port 2525.");
        }
        else if (port is not (465 or 587) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMTP credentials and port 465 (implicit TLS) or 587 (STARTTLS) are required.");
        }

        var message = new MimeMessage();
        string from = smtp4dev
            ? Environment.GetEnvironmentVariable("DANTOPHONG_SMTP_FROM") ?? "test@dantophong.invalid"
            : user.Trim();
        message.From.Add(new MailboxAddress("Scooter Racing", from));
        message.To.Add(MailboxAddress.Parse(email.Trim()));
        message.Subject = passwordReset ? "Scooter Racing - Password Reset Code" : "Scooter Racing - Registration Code";
        message.Body = new TextPart("plain")
        {
            Text = passwordReset
                ? $"Your password reset code is {code}. It expires in 10 minutes."
                : $"Your registration code is {code}. It expires in 10 minutes."
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var client = new SmtpClient
        {
            Timeout = 15000
        };
        SecureSocketOptions socketOptions = smtp4dev
            ? SecureSocketOptions.None
            : port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        await client.ConnectAsync(host, port, socketOptions, timeout.Token).ConfigureAwait(false);
        if (!smtp4dev)
            await client.AuthenticateAsync(user.Trim(), password.Replace(" ", "").Trim(), timeout.Token).ConfigureAwait(false);
        await client.SendAsync(FormatOptions.Default, new SmtpSendRequest(message), timeout.Token).ConfigureAwait(false);
        await client.DisconnectAsync(true, timeout.Token).ConfigureAwait(false);
    }
}
