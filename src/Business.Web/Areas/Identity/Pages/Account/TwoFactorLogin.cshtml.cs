using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Business.Infrastructure.Identity;

namespace Business.Web.Areas.Identity.Pages.Account;

public class TwoFactorLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public TwoFactorLoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

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

        var code = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, RememberMe, Input.RememberMachine);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Hesap geçici olarak kilitlendi.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Doğrulama kodu geçersiz.");
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "Doğrulama kodu en az 6 karakter olmalıdır.")]
        [Display(Name = "Doğrulama Kodu")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Bu cihazı hatırla")]
        public bool RememberMachine { get; set; }
    }
}
