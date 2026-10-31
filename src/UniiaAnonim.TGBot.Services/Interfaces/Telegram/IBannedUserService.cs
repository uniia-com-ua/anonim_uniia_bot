namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines operations for checking and managing banned users with anti-DDoS in-memory caching.
/// </summary>
public interface IBannedUserService
{
    /// <summary>
    /// Checks whether the user with the specified Telegram ID is currently banned.
    /// Employs in-memory caching to protect the database against DDoS attacks.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the user is banned; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsBannedAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Bans the specified user, persists the ban to the database, and updates the local cache.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to ban.</param>
    /// <param name="reason">An optional reason for banning the user.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous ban operation.</returns>
    Task BanUserAsync(long telegramId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Removes the ban for the specified user, removes the record from the database,
    /// and evicts the entry from the in-memory cache.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to unban.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous unban operation.</returns>
    Task UnbanUserAsync(long telegramId, CancellationToken ct = default);
}
