using System.Text.Json;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Business.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramNotificationService _telegramNotificationService;
    private readonly TelegramBotOptions _telegramBotOptions;
    private readonly ILogger<TelegramController> _logger;

    public TelegramController(
        ITelegramNotificationService telegramNotificationService,
        IOptions<TelegramBotOptions> telegramBotOptions,
        ILogger<TelegramController> logger)
    {
        _telegramNotificationService = telegramNotificationService;
        _telegramBotOptions = telegramBotOptions.Value;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_telegramBotOptions.WebhookSecret))
        {
            var receivedSecret = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.Equals(receivedSecret, _telegramBotOptions.WebhookSecret, StringComparison.Ordinal))
            {
                return Unauthorized();
            }
        }

        if (!TryReadIncomingMessage(payload, out var telegramChatId, out var telegramUsername, out var text))
        {
            return Ok(new { ok = true });
        }

        try
        {
            var responseMessage = await _telegramNotificationService.ProcessIncomingTextMessageAsync(
                telegramChatId!,
                telegramUsername,
                text!,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(responseMessage))
            {
                await _telegramNotificationService.SendDirectMessageAsync(telegramChatId!, responseMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook işlenirken hata oluştu.");
        }

        return Ok(new { ok = true });
    }

    private static bool TryReadIncomingMessage(JsonElement payload, out string? telegramChatId, out string? telegramUsername, out string? text)
    {
        telegramChatId = null;
        telegramUsername = null;
        text = null;

        if (!payload.TryGetProperty("message", out var message))
        {
            return false;
        }

        if (!message.TryGetProperty("chat", out var chat) ||
            !chat.TryGetProperty("id", out var chatIdElement))
        {
            return false;
        }

        telegramChatId = chatIdElement.ToString();

        if (message.TryGetProperty("from", out var from) &&
            from.TryGetProperty("username", out var usernameElement))
        {
            telegramUsername = usernameElement.GetString();
        }

        if (message.TryGetProperty("text", out var textElement))
        {
            text = textElement.GetString();
        }

        return !string.IsNullOrWhiteSpace(telegramChatId) && !string.IsNullOrWhiteSpace(text);
    }
}
