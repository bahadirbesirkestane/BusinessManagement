using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Business.Web.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
