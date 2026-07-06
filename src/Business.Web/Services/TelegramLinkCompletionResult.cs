namespace Business.Web.Services;

public sealed class TelegramLinkCompletionResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserDisplayName { get; init; }
}
