using Microsoft.AspNetCore.Identity;

namespace Business.Infrastructure.Identity;

public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email)
    {
        return Error(nameof(DuplicateEmail), $"'{email}' e-posta adresi zaten kullanılıyor.");
    }

    public override IdentityError DuplicateUserName(string userName)
    {
        return Error(nameof(DuplicateUserName), $"'{userName}' kullanıcı adı zaten kullanılıyor.");
    }

    public override IdentityError InvalidEmail(string? email)
    {
        return Error(nameof(InvalidEmail), $"'{email}' geçerli bir e-posta adresi değildir.");
    }

    public override IdentityError InvalidUserName(string? userName)
    {
        return Error(nameof(InvalidUserName), $"'{userName}' geçerli bir kullanıcı adı değildir.");
    }

    public override IdentityError PasswordRequiresDigit()
    {
        return Error(nameof(PasswordRequiresDigit), "Parola en az bir rakam içermelidir.");
    }

    public override IdentityError PasswordRequiresLower()
    {
        return Error(nameof(PasswordRequiresLower), "Parola en az bir küçük harf içermelidir.");
    }

    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return Error(nameof(PasswordRequiresNonAlphanumeric), "Parola en az bir özel karakter içermelidir.");
    }

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return Error(nameof(PasswordRequiresUniqueChars), $"Parola en az {uniqueChars} farklı karakter içermelidir.");
    }

    public override IdentityError PasswordRequiresUpper()
    {
        return Error(nameof(PasswordRequiresUpper), "Parola en az bir büyük harf içermelidir.");
    }

    public override IdentityError PasswordTooShort(int length)
    {
        return Error(nameof(PasswordTooShort), $"Parola en az {length} karakter olmalıdır.");
    }

    public override IdentityError UserAlreadyHasPassword()
    {
        return Error(nameof(UserAlreadyHasPassword), "Kullanıcının zaten bir parolası var.");
    }

    private static IdentityError Error(string code, string description)
    {
        return new IdentityError
        {
            Code = code,
            Description = description
        };
    }
}
