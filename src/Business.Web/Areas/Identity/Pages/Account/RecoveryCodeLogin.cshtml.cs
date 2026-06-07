using System.ComponentModel.DataAnnotations;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Business.Web.Areas.Identity.Pages.Account;

public class RecoveryCodeLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RecoveryCodeLoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToPage("./Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToPage("./Login");
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        ModelState.AddModelError(string.Empty, "Kurtarma kodu geçersiz veya daha önce kullanılmış.");
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Kurtarma kodu zorunludur.")]
        [Display(Name = "Kurtarma Kodu")]
        public string RecoveryCode { get; set; } = string.Empty;
    }
}
