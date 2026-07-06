using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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
            if (message.RequireConfiguredDelivery)
            {
                throw new InvalidOperationException("SMTP ayarları eksik olduğu için e-posta gönderilemedi.");
            }

            _logger.LogInformation(
                "SMTP ayarı bulunamadı. E-posta log'a yazıldı. Alıcı: {To}, Konu: {Subject}, İçerik: {Body}",
                message.To,
                message.Subject,
                message.TextBody ?? message.HtmlBody);
            return;
        }

        var recipients = message.ToList?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        if (recipients.Count == 0 && !string.IsNullOrWhiteSpace(message.To))
        {
            recipients.Add(message.To);
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_options.FromName ?? _options.FromEmail, _options.FromEmail));
        foreach (var recipient in recipients.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            email.To.Add(MailboxAddress.Parse(recipient));
        }

        email.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };

        foreach (var attachment in message.Attachments)
        {
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        email.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = GetSecureSocketOptions();
        await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private SecureSocketOptions GetSecureSocketOptions()
    {
        if (!_options.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return _options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}
