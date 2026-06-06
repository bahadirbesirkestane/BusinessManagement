using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Business.Web.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogInformation(
                "SMTP ayarı bulunamadı. E-posta log'a yazıldı. Alıcı: {To}, Konu: {Subject}, İçerik: {Body}",
                message.To,
                message.Subject,
                message.TextBody ?? message.HtmlBody);
            return;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(message.To);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
