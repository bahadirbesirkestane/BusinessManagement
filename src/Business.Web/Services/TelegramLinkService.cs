using System.Security.Cryptography;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Services;

public sealed class TelegramLinkService : ITelegramLinkService
{
    private static readonly char[] CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private readonly ApplicationDbContext _context;

    public TelegramLinkService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TelegramUserLinkRequest> CreateOrRefreshAsync(string userId, int ttlMinutes, CancellationToken cancellationToken = default)
    {
        var activeRequest = await _context.TelegramUserLinkRequests
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.CompletedAt == null &&
                     x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (activeRequest is not null)
        {
            activeRequest.Code = GenerateCode();
            activeRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
            await _context.SaveChangesAsync(cancellationToken);
            return activeRequest;
        }

        var request = new TelegramUserLinkRequest
        {
            UserId = userId,
            Code = GenerateCode(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes)
        };

        _context.TelegramUserLinkRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<TelegramUserLinkRequest?> GetActiveRequestAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _context.TelegramUserLinkRequests
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.CompletedAt == null &&
                     x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<TelegramLinkCompletionResult> CompleteLinkAsync(string code, string telegramChatId, string? telegramUsername, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var request = await _context.TelegramUserLinkRequests
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);

        if (request is null)
        {
            return new TelegramLinkCompletionResult
            {
                Message = "Bu bağlama kodu bulunamadı. Lütfen uygulamadan yeni bir kod üretip tekrar deneyin."
            };
        }

        if (request.CompletedAt.HasValue)
        {
            return new TelegramLinkCompletionResult
            {
                Message = "Bu bağlama kodu daha önce kullanılmış. Lütfen uygulamadan yeni bir kod üretin."
            };
        }

        if (request.ExpiresAt <= DateTime.UtcNow)
        {
            return new TelegramLinkCompletionResult
            {
                Message = "Bağlama kodunun süresi dolmuş. Lütfen uygulamadan yeni bir kod üretin."
            };
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return new TelegramLinkCompletionResult
            {
                Message = "Bu bağlama koduna ait kullanıcı bulunamadı. Lütfen sistem yöneticinizle görüşün."
            };
        }

        var existingUser = await _context.Users
            .Where(x => x.TelegramChatId == telegramChatId && x.Id != user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is not null)
        {
            existingUser.TelegramChatId = null;
            existingUser.TelegramUsername = null;
            existingUser.TelegramLinkedAt = null;
            existingUser.TelegramNotificationsEnabled = false;
        }

        user.TelegramChatId = telegramChatId;
        user.TelegramUsername = string.IsNullOrWhiteSpace(telegramUsername)
            ? null
            : telegramUsername.Trim().TrimStart('@');
        user.TelegramLinkedAt = DateTime.UtcNow;
        user.TelegramNotificationsEnabled = true;

        request.TelegramChatId = telegramChatId;
        request.TelegramUsername = user.TelegramUsername;
        request.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new TelegramLinkCompletionResult
        {
            Succeeded = true,
            UserDisplayName = user.FullName ?? user.Email ?? user.UserName ?? user.Id,
            Message = "Telegram hesabınız başarıyla bağlandı. Artık görev bildirimlerini bu bottan alabilirsiniz."
        };
    }

    private static string GenerateCode()
    {
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);

        var chars = new char[randomBytes.Length];
        for (var i = 0; i < randomBytes.Length; i++)
        {
            chars[i] = CodeAlphabet[randomBytes[i] % CodeAlphabet.Length];
        }

        return new string(chars);
    }
}
