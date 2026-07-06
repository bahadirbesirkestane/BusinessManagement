namespace Business.Web.Services;

public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public IReadOnlyList<string>? ToList { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
    public bool RequireConfiguredDelivery { get; init; }
}
