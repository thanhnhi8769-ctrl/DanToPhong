using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UI
{
    internal static class EmailVerificationService
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;

        private const string SenderEmail = "thanhnhi8769@gmail.com";
        private const string AppPassword = "wqljstnxicbxizde";

        public static string GenerateCode6Digits()
        {
            int code = RandomNumberGenerator.GetInt32(100000, 1000000);
            return code.ToString("D6");
        }

        public static async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(SenderEmail) ||
                SenderEmail.StartsWith("your_", StringComparison.OrdinalIgnoreCase) ||
                SenderEmail.Equals("gmail_cua_ban@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(AppPassword) ||
                AppPassword.StartsWith("your_", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình SenderEmail hoặc AppPassword 16 ký tự trong EmailVerificationService.cs.");
            }

            using var client = new SmtpClient(SmtpHost, SmtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(SenderEmail.Trim(), AppPassword.Replace(" ", "").Trim()),
                Timeout = 15000
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(SenderEmail.Trim(), "Scooter Racing"),
                Subject = "Scooter Racing - Verification Code",
                Body = $"Your 6-digit verification code is: {code}\nThis code will expire in 60 seconds.",
                IsBodyHtml = false
            };
            mail.To.Add(toEmail.Trim());

            await client.SendMailAsync(mail).ConfigureAwait(false);
        }
    }
}