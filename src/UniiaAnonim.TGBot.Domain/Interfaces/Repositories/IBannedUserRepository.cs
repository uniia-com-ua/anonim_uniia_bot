using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

/// <summary>
/// Defines repository operations for managing <see cref="BannedUser"/> entities.
/// </summary>
public interface IBannedUserRepository
    : IGenericRepository<BannedUser>
{
    /// <summary>
    /// Asynchronously checks whether the user with the specified Telegram ID hash is banned.
    /// </summary>
    /// <param name="telegramIdHash">The hashed Telegram user ID to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the user is banned; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsBannedAsync(string telegramIdHash, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously records a ban for the specified Telegram user ID hash.
    /// </summary>
    /// <param name="telegramIdHash">The hashed Telegram user ID to ban.</param>
    /// <param name="reason">An optional reason for banning the user.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BanUserAsync(string telegramIdHash, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously removes the ban for the specified Telegram user ID hash.
    /// </summary>
    /// <param name="telegramIdHash">The hashed Telegram user ID to unban.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnbanUserAsync(string telegramIdHash, CancellationToken ct = default);
}
