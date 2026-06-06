using System.ComponentModel.DataAnnotations;
using System.Text;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Business.Web.Areas.Identity.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed)
        {
            ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        Input.UserId = user.Id;
        Input.Token = token;
        Input.Email = user.Email ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user is null || !user.EmailConfirmed)
        {
            ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var token = DecodeToken(Input.Token);
        if (token is null)
        {
            ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, token, Input.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(x => x.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await _userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = "Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToPage("/Account/Login", new { area = "Identity" });
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
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni şifre 8 ile 100 karakter arasında olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifre tekrarı eşleşmiyor.")]
        [Display(Name = "Yeni Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
