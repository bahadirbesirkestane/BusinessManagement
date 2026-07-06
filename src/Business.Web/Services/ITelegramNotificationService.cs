namespace Business.Web.Services;

public interface ITelegramNotificationService
{
    Task<TelegramDispatchResult> SendMessageToUsersAsync(
        IEnumerable<string> userIds,
        string message,
        bool includeUsersWithNotificationsDisabled = false,
        CancellationToken cancellationToken = default);
    Task<string> ProcessIncomingTextMessageAsync(string telegramChatId, string? telegramUsername, string text, CancellationToken cancellationToken = default);
    Task SendDirectMessageAsync(string telegramChatId, string message, CancellationToken cancellationToken = default);
}
