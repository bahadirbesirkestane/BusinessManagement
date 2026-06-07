using System.Text;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ConfirmEmailChangeModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            ErrorMessage = "E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            ErrorMessage = "E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        var result = await _userManager.ChangeEmailAsync(user, email, decodedToken);
        if (!result.Succeeded)
        {
            ErrorMessage = "E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.";
            return Page();
        }

        user.UserName = email;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            ErrorMessage = "E-posta güncellendi ancak kullanıcı adı güncellenemedi. Lütfen sistem yöneticisiyle görüşün.";
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "E-posta adresiniz doğrulandı ve güncellendi.";
        return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
    }
}
