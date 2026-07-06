namespace Business.Web.Services;

public sealed class TelegramDispatchResult
{
    public bool IsEnabled { get; init; }
    public bool IsConfigured { get; init; }
    public int RequestedRecipientCount { get; init; }
    public int EligibleRecipientCount { get; init; }
    public int SentRecipientCount { get; init; }
    public IReadOnlyList<string> MissingChatRecipients { get; init; } = [];
    public IReadOnlyList<string> FailedRecipients { get; init; } = [];
}
