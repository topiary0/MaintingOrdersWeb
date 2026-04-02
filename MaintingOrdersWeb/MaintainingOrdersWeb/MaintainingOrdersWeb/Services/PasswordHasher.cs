using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MaintainingOrdersWeb.Services;

public static class PasswordHasher
{
    private static readonly Regex Sha256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    public static string Hash(string password)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        StringBuilder builder = new();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    public static bool IsSha256Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Sha256Regex.IsMatch(value);
    }
}
