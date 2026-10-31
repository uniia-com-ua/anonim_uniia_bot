using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Application.Services.Telegram;

/// <summary>
/// Implements the <see cref="IBannedUserService"/> with Anti-DDoS in-memory caching
/// and cryptographic hashing of sensitive user identifiers.
/// </summary>
/// <param name="bannedUserRepository">The banned user repository.</param>
/// <param name="hashService">The cryptographic hashing service.</param>
/// <param name="memoryCache">The in-memory cache provider.</param>
/// <param name="logger">The logger instance.</param>
public sealed partial class BannedUserService(
    IBannedUserRepository bannedUserRepository,
    IHashService hashService,
    IMemoryCache memoryCache,
    ILogger<BannedUserService> logger)
    : IBannedUserService
{
    private static readonly TimeSpan BanCacheDuration = TimeSpan.FromHours(1);

    /// <inheritdoc/>
    public async Task<bool> IsBannedAsync(long telegramId, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(telegramId);

        if (memoryCache.TryGetValue(cacheKey, out bool isBanned) && isBanned)
        {
            return true;
        }

        var telegramIdHash = ComputeTelegramIdHash(telegramId);
        var bannedInDb = await bannedUserRepository.IsBannedAsync(telegramIdHash, ct);
        if (bannedInDb)
        {
            LogUserConfirmedBannedInDb(logger, telegramIdHash);

            memoryCache.Set(cacheKey, true, BanCacheDuration);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task BanUserAsync(long telegramId, string? reason = null, CancellationToken ct = default)
    {
        var telegramIdHash = ComputeTelegramIdHash(telegramId);
        await bannedUserRepository.BanUserAsync(telegramIdHash, reason, ct);
        memoryCache.Set(GetCacheKey(telegramId), true, BanCacheDuration);

        LogUserBannedAddedToCache(logger, telegramIdHash);
    }

    /// <inheritdoc/>
    public async Task UnbanUserAsync(long telegramId, CancellationToken ct = default)
    {
        var telegramIdHash = ComputeTelegramIdHash(telegramId);
        await bannedUserRepository.UnbanUserAsync(telegramIdHash, ct);
        memoryCache.Remove(GetCacheKey(telegramId));

        LogUserUnbannedRemovedFromCache(logger, telegramIdHash);
    }

    private string ComputeTelegramIdHash(long telegramId) =>
        hashService.ComputeHash(telegramId.ToString(CultureInfo.InvariantCulture));

    private static string GetCacheKey(long telegramId) => $"banned_user_{telegramId}";

    [LoggerMessage(
        EventId = LogEventIds.UserConfirmedBannedInDb,
        Level = LogLevel.Information,
        Message = "User {TelegramIdHash} confirmed banned in DB. Caching status for Anti-DDoS protection.")]
    private static partial void LogUserConfirmedBannedInDb(ILogger logger, string telegramIdHash);

    [LoggerMessage(
        EventId = LogEventIds.UserBannedAddedToCache,
        Level = LogLevel.Information,
        Message = "User {TelegramIdHash} has been banned and added to memory cache.")]
    private static partial void LogUserBannedAddedToCache(ILogger logger, string telegramIdHash);

    [LoggerMessage(
        EventId = LogEventIds.UserUnbannedRemovedFromCache,
        Level = LogLevel.Information,
        Message = "User {TelegramIdHash} has been unbanned and removed from memory cache.")]
    private static partial void LogUserUnbannedRemovedFromCache(ILogger logger, string telegramIdHash);
}
