using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Infrastructure.Persistence;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

/// <summary>
/// Provides repository implementation for managing <see cref="BannedUser"/> entities.
/// </summary>
/// <param name="appDbContext">The application database context.</param>
public sealed class BannedUserRepository(
    AppDbContext appDbContext)
    : GenericRepository<BannedUser>(appDbContext),
    IBannedUserRepository
{
    /// <inheritdoc/>
    public async Task<bool> IsBannedAsync(string telegramIdHash, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .AnyAsync(x => x.TelegramIdHash == telegramIdHash, ct);

    /// <inheritdoc/>
    public async Task BanUserAsync(string telegramIdHash, string? reason = null, CancellationToken ct = default)
    {
        var exists = await DbSet.AsNoTracking().AnyAsync(x => x.TelegramIdHash == telegramIdHash, ct);
        if (!exists)
        {
            var newBannedUser = new BannedUser
            {
                TelegramIdHash = telegramIdHash,
                Reason = reason,
                CreatedAt = DateTime.UtcNow,
            };

            DbSet.Add(newBannedUser);
            await Context.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task UnbanUserAsync(string telegramIdHash, CancellationToken ct = default)
    {
        await DbSet.Where(x => x.TelegramIdHash == telegramIdHash).ExecuteDeleteAsync(ct);
    }
}
