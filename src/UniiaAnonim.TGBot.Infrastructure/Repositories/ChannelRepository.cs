using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Infrastructure.Persistence;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

internal class ChannelRepository(AppDbContext appDbContext)
    : GenericRepository<Channel>(appDbContext),
    IChannelRepository
{
    /// <summary>
    /// Asynchronously checks whether a channel with the specified unique identifier exists in the database.
    /// </summary>
    /// <param name="channelId">The unique identifier of the channel to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the channel exists; otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> ExistsAsync(long channelId, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.ChannelId == channelId, ct);

    /// <summary>
    /// Asynchronously retrieves the first channel of the specified type from the database.
    /// </summary>
    /// <param name="type">The type of the channel to retrieve.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the first retrieved channel,
    /// or <see langword="null"/> if no matching channel is found.
    /// </returns>
    public async Task<Channel?> GetByTypeAsync(ChannelType type, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Type == type, ct);

    /// <summary>
    /// Asynchronously deletes a channel from the database based on its unique identifier.
    /// </summary>
    /// <param name="channelId">The unique identifier of the channel to delete.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deletion operation.</returns>
    public async Task DeleteByChannelIdAsync(long channelId, CancellationToken ct = default)
        => await DbSet
                .Where(x => x.ChannelId == channelId)
                .ExecuteDeleteAsync(ct);
}
