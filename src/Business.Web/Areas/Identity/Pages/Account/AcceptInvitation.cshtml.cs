using System.ComponentModel.DataAnnotations;
using System.Text;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;

namespace Business.Web.Areas.Identity.Pages.Account;

public class AcceptInvitationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AcceptInvitationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? emailToken, string? passwordToken)
    {
        var user = await FindInvitedUserAsync(userId, emailToken, passwordToken);
        if (user is null)
        {
            ErrorMessage = "Davet bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        Input.UserId = user.Id;
        Input.EmailToken = emailToken!;
        Input.PasswordToken = passwordToken!;
        Input.Email = user.Email ?? string.Empty;
        Input.FullName = user.FullName ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await FindInvitedUserAsync(Input.UserId, Input.EmailToken, Input.PasswordToken);
        if (user is null)
        {
            ErrorMessage = "Davet bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        Input.Email = user.Email ?? string.Empty;
        Input.FullName = user.FullName ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!user.EmailConfirmed)
        {
            var emailToken = DecodeToken(Input.EmailToken);
            if (emailToken is null)
            {
                ErrorMessage = "Davet bağlantısı geçersiz veya süresi dolmuş.";
                return Page();
            }

            var emailResult = await _userManager.ConfirmEmailAsync(user, emailToken);
            if (!emailResult.Succeeded)
            {
                ErrorMessage = "Davet bağlantısı geçersiz veya süresi dolmuş.";
                return Page();
            }
        }

        var passwordToken = DecodeToken(Input.PasswordToken);
        if (passwordToken is null)
        {
            ErrorMessage = "Davet bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var passwordResult = await _userManager.ResetPasswordAsync(user, passwordToken, Input.Password);
        if (!passwordResult.Succeeded)
        {
            foreach (var error in passwordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        user.IsActive = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        TempData["Success"] = "Hesabınız doğrulandı. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }

    private async Task<ApplicationUser?> FindInvitedUserAsync(string? userId, string? emailToken, string? passwordToken)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(emailToken) ||
            string.IsNullOrWhiteSpace(passwordToken))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsActive)
        {
            return null;
        }

        return user;
    }

    private static string? DecodeToken(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public class InputModel
    {
        public string UserId { get; set; } = string.Empty;
        public string EmailToken { get; set; } = string.Empty;
        public string PasswordToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre 8 ile 100 karakter arasında olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifre tekrarı eşleşmiyor.")]
        [Display(Name = "Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
