using Business.Domain.Entities;

namespace Business.Web.Services;

public interface ITelegramLinkService
{
    Task<TelegramUserLinkRequest> CreateOrRefreshAsync(string userId, int ttlMinutes, CancellationToken cancellationToken = default);
    Task<TelegramUserLinkRequest?> GetActiveRequestAsync(string userId, CancellationToken cancellationToken = default);
    Task<TelegramLinkCompletionResult> CompleteLinkAsync(string code, string telegramChatId, string? telegramUsername, CancellationToken cancellationToken = default);
}
