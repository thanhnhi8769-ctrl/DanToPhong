using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DanToPhong.Protocol
{
    public static class CertificatePinValidator
    {
        public static string NormalizeSha256Thumbprint(string? thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                return string.Empty;

            string normalized = new string(thumbprint
                .Where(character => character != ':' && character != '-' && !char.IsWhiteSpace(character))
                .ToArray())
                .ToUpperInvariant();
            if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
                throw new ArgumentException("Certificate SHA-256 thumbprint must contain exactly 64 hexadecimal characters.", nameof(thumbprint));
            return normalized;
        }

        public static bool IsValidSha256Thumbprint(string? thumbprint)
        {
            try
            {
                return NormalizeSha256Thumbprint(thumbprint).Length == 64;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool Validate(
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors policyErrors,
            string? expectedThumbprint)
        {
            if (certificate == null)
                return false;

            string expected;
            try
            {
                expected = NormalizeSha256Thumbprint(expectedThumbprint);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (expected.Length == 0)
                return false;

            using (SHA256 sha256 = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha256.ComputeHash(certificate.GetRawCertData()))
                    .Replace("-", string.Empty);
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (policyErrors == SslPolicyErrors.None)
                return true;
            if (policyErrors != SslPolicyErrors.RemoteCertificateChainErrors || chain == null)
                return false;

            X509ChainStatus[] statuses = chain.ChainStatus;
            return statuses.Length > 0 && statuses.All(status => status.Status == X509ChainStatusFlags.UntrustedRoot);
        }
    }
}