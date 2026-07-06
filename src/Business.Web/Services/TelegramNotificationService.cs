using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Business.Web.Services;

public sealed class TelegramNotificationService : ITelegramNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly ITelegramLinkService _telegramLinkService;
    private readonly TelegramBotOptions _botOptions;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        ITelegramLinkService telegramLinkService,
        IOptions<TelegramBotOptions> botOptions,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _telegramLinkService = telegramLinkService;
        _botOptions = botOptions.Value;
        _logger = logger;
    }

    public async Task<TelegramDispatchResult> SendMessageToUsersAsync(
        IEnumerable<string> userIds,
        string message,
        bool includeUsersWithNotificationsDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = userIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var settings = await GetSettingsAsync(cancellationToken);
        if (settings is null || !settings.IsEnabled)
        {
            return new TelegramDispatchResult
            {
                RequestedRecipientCount = requestedIds.Count,
                IsEnabled = false,
                IsConfigured = !string.IsNullOrWhiteSpace(_botOptions.Token)
            };
        }

        if (string.IsNullOrWhiteSpace(_botOptions.Token))
        {
            return new TelegramDispatchResult
            {
                RequestedRecipientCount = requestedIds.Count,
                IsEnabled = true,
                IsConfigured = false
            };
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(x => requestedIds.Contains(x.Id) && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var eligibleUsers = includeUsersWithNotificationsDisabled
            ? users
            : users.Where(x => x.TelegramNotificationsEnabled).ToList();

        var missingChatRecipients = eligibleUsers
            .Where(x => string.IsNullOrWhiteSpace(x.TelegramChatId))
            .Select(GetDisplayName)
            .ToList();

        var sendableUsers = eligibleUsers
            .Where(x => !string.IsNullOrWhiteSpace(x.TelegramChatId))
            .ToList();

        var failedRecipients = new List<string>();
        var sentCount = 0;

        foreach (var user in sendableUsers)
        {
            try
            {
                await SendRawMessageAsync(user.TelegramChatId!, message, cancellationToken);
                sentCount++;
            }
            catch (Exception ex)
            {
                failedRecipients.Add(GetDisplayName(user));
                _logger.LogWarning(ex, "Telegram mesajı gönderilemedi. UserId: {UserId}", user.Id);
            }
        }

        return new TelegramDispatchResult
        {
            IsEnabled = true,
            IsConfigured = true,
            RequestedRecipientCount = requestedIds.Count,
            EligibleRecipientCount = eligibleUsers.Count,
            SentRecipientCount = sentCount,
            MissingChatRecipients = missingChatRecipients,
            FailedRecipients = failedRecipients
        };
    }

    public async Task<string> ProcessIncomingTextMessageAsync(string telegramChatId, string? telegramUsername, string text, CancellationToken cancellationToken = default)
    {
        var trimmedText = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            return "Bağlama kodunuzu mesaj olarak gönderebilirsiniz. Örnek: ABCD1234";
        }

        var candidateCode = ExtractLinkCode(trimmedText);
        if (string.IsNullOrWhiteSpace(candidateCode))
        {
            return "Bağlama için uygulamadaki Hesap Yönetimi ekranından ürettiğiniz kodu gönderin. Örnek: ABCD1234";
        }

        var result = await _telegramLinkService.CompleteLinkAsync(candidateCode, telegramChatId, telegramUsername, cancellationToken);
        return result.Succeeded && !string.IsNullOrWhiteSpace(result.UserDisplayName)
            ? $"{result.UserDisplayName}, {result.Message}"
            : result.Message;
    }

    public async Task SendDirectMessageAsync(string telegramChatId, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_botOptions.Token))
        {
            throw new InvalidOperationException("Telegram bot token tanımlanmadığı için mesaj gönderilemedi.");
        }

        await SendRawMessageAsync(telegramChatId, message, cancellationToken);
    }

    private async Task<Business.Domain.Entities.TelegramNotificationSetting?> GetSettingsAsync(CancellationToken cancellationToken)
    {
        return await _context.TelegramNotificationSettings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SendRawMessageAsync(string telegramChatId, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("TelegramBot");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.telegram.org/bot{_botOptions.Token}/sendMessage");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                chat_id = telegramChatId,
                text = message
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string? ExtractLinkCode(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        if (parts[0].Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            return parts.Length > 1 ? parts[1].Trim() : null;
        }

        return parts[0].Trim();
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        return user.FullName ?? user.Email ?? user.UserName ?? user.Id;
    }
}
