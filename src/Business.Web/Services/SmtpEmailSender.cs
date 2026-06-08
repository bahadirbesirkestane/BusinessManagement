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
            _logger.LogInformation(
                "SMTP ayarı bulunamadı. E-posta log'a yazıldı. Alıcı: {To}, Konu: {Subject}, İçerik: {Body}",
                message.To,
                message.Subject,
                message.TextBody ?? message.HtmlBody);
            return;
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_options.FromName ?? _options.FromEmail, _options.FromEmail));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;
        email.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        }.ToMessageBody();

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
