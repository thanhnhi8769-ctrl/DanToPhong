using System.Text.RegularExpressions;

namespace DanToPhong.Protocol;

public static class AccountInputValidation
{
    public const int MaxEmailLength = 100;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordHasLetter = new(@"[A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex PasswordHasDigit = new(@"[0-9]", RegexOptions.Compiled);

    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Length <= MaxEmailLength &&
        EmailPattern.IsMatch(email);

    public static bool IsValidPassword(string? password) =>
        password is { Length: >= MinPasswordLength and <= MaxPasswordLength } &&
        PasswordHasLetter.IsMatch(password) &&
        PasswordHasDigit.IsMatch(password);
}
