using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Business.Web.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && user.EmailConfirmed)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, token = encodedToken },
                protocol: Request.Scheme) ?? string.Empty;

            try
            {
                await _emailSender.SendAsync(new EmailMessage
                {
                    To = email,
                    Subject = "FirmaTakip şifre sıfırlama",
                    HtmlBody = $"""
                        <p>Merhaba {HtmlEncoder.Default.Encode(user.FullName ?? user.Email ?? "Kullanıcı")},</p>
                        <p>FirmaTakip hesabınız için şifre sıfırlama talebi alındı.</p>
                        <p><a href="{HtmlEncoder.Default.Encode(resetLink)}">Yeni şifre belirle</a></p>
                        <p>Bu işlemi siz başlatmadıysanız bu e-postayı yok sayabilirsiniz.</p>
                        """,
                    TextBody = $"""
                        Merhaba {user.FullName ?? user.Email ?? "Kullanıcı"},

                        FirmaTakip hesabınız için şifre sıfırlama talebi alındı.
                        Şifre sıfırlama linki: {resetLink}

                        Bu işlemi siz başlatmadıysanız bu e-postayı yok sayabilirsiniz.
                        """
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi. UserId: {UserId}", user.Id);
            }
        }

        return RedirectToPage("/Account/ForgotPasswordConfirmation", new { area = "Identity" });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
    }
}
