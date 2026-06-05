namespace Business.Application.Services;

public interface IAdminRecoveryCodeService
{
    IReadOnlyList<string> GeneratePlainCodes(int count);
    string CreateSalt();
    string HashCode(string code, string salt);
    bool VerifyCode(string code, string salt, string expectedHash);
    string NormalizeCode(string code);
}
