using System.Security.Cryptography;
using System.Text;
using Business.Application.Services;

namespace Business.Infrastructure.Services;

public class AdminRecoveryCodeService : IAdminRecoveryCodeService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public IReadOnlyList<string> GeneratePlainCodes(int count)
    {
        var codes = new List<string>();
        for (var i = 0; i < count; i++)
        {
            codes.Add(GenerateCode());
        }

        return codes;
    }

    public string CreateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    public string HashCode(string code, string salt)
    {
        var normalizedCode = NormalizeCode(code);
        var payload = Encoding.UTF8.GetBytes($"{salt}:{normalizedCode}");
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    public bool VerifyCode(string code, string salt, string expectedHash)
    {
        var actualHash = HashCode(code, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(actualHash),
            Convert.FromHexString(expectedHash));
    }

    public string NormalizeCode(string code)
    {
        return new string(code
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string GenerateCode()
    {
        var chars = new char[20];
        var bytes = RandomNumberGenerator.GetBytes(chars.Length);
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        var raw = new string(chars);
        return string.Join("-", Enumerable.Range(0, 4).Select(index => raw.Substring(index * 5, 5)));
    }
}
