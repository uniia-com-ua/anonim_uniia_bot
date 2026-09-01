using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

public interface IChannelRepository
    : IGenericRepository<Channel>
{
    /// <summary>
    /// Asynchronously checks whether a channel with the specified unique identifier exists in the database.
    /// </summary>
    /// <param name="channelId">The unique identifier of the channel to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the channel exists; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> ExistsAsync(long channelId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves the first channel of the specified type from the database.
    /// </summary>
    /// <param name="type">The type of the channel to retrieve.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the first retrieved channel,
    /// or <see langword="null"/> if no matching channel is found.
    /// </returns>
    Task<Channel?> GetByTypeAsync(ChannelType type, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously deletes a channel from the database based on its unique identifier.
    /// </summary>
    /// <param name="channelId">The unique identifier of the channel to delete.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deletion operation.</returns>
    Task DeleteByChannelIdAsync(long channelId, CancellationToken ct = default);
}