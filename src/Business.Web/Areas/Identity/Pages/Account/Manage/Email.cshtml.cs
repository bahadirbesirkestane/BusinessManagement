using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class EmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailModel> _logger;

    public EmailModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ILogger<EmailModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string CurrentEmail { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        Load(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        Load(user);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var newEmail = Input.NewEmail.Trim();
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Input.NewEmail), "Yeni e-posta mevcut e-posta ile aynı olamaz.");
            return Page();
        }

        var existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            ModelState.AddModelError(nameof(Input.NewEmail), "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            return Page();
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = Url.Page(
            "/Account/Manage/ConfirmEmailChange",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, email = newEmail, token = encodedToken },
            protocol: Request.Scheme) ?? string.Empty;

        try
        {
            await _emailSender.SendAsync(new EmailMessage
            {
                To = newEmail,
                Subject = "FirmaTakip e-posta değişikliği doğrulama",
                HtmlBody = $"""
                    <p>Merhaba {HtmlEncoder.Default.Encode(user.FullName ?? user.Email ?? "Kullanıcı")},</p>
                    <p>FirmaTakip hesabınız için e-posta değişikliği talebi alındı.</p>
                    <p><a href="{HtmlEncoder.Default.Encode(confirmationLink)}">Yeni e-postamı doğrula</a></p>
                    <p>Bu işlemi siz başlatmadıysanız bu e-postayı yok sayabilirsiniz.</p>
                    """,
                TextBody = $"""
                    Merhaba {user.FullName ?? user.Email ?? "Kullanıcı"},

                    FirmaTakip hesabınız için e-posta değişikliği talebi alındı.
                    Doğrulama linki: {confirmationLink}

                    Bu işlemi siz başlatmadıysanız bu e-postayı yok sayabilirsiniz.
                    """
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta değişikliği doğrulama maili gönderilemedi. UserId: {UserId}", user.Id);
            TempData["Error"] = "Doğrulama e-postası gönderilemedi. SMTP ayarlarını kontrol edin.";
            return RedirectToPage();
        }

        TempData["Success"] = "Yeni e-posta adresinize doğrulama bağlantısı gönderildi.";
        return RedirectToPage();
    }

    private void Load(ApplicationUser user)
    {
        CurrentEmail = user.Email ?? string.Empty;
        EmailConfirmed = user.EmailConfirmed;
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Yeni e-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
        [Display(Name = "Yeni E-posta")]
        public string NewEmail { get; set; } = string.Empty;
    }
}
