namespace Business.Web.Services;

public sealed class TelegramBotOptions
{
    public const string SectionName = "Telegram:Bot";

    public string Token { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
