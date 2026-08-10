using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

/// <summary>
/// Defines repository methods for managing story authors and querying story records.
/// </summary>
public interface IStoryAuthorRepository
    : IGenericRepository<StoryAuthor>
{
    /// <summary>
    /// Asynchronously checks whether an unpublished entity with the specified author identifier hash exists in the database.
    /// </summary>
    /// <param name="authorIdHash">The unique author identifier hash to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if at least one unpublished entity exists for the author hash; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> ExistsAsync(string authorIdHash, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks whether an entity with the specified story identifier exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the story to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the entity exists; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves the actual (latest unpublished) channel message identifier for the specified author identifier.
    /// </summary>
    /// <param name="authorIdHash">The unique author identifier to search for.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the channel message identifier (<see cref="int"/>).
    /// </returns>
    /// <exception cref="EntityNotFoundException">Thrown when no unpublished story is found for the specified author.</exception>
    Task<int> GetActualStoryMessageIdAsync(string authorIdHash, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously marks a story author record as published based on its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to update.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task MarkAsPublishedAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks if a story author record is marked as published.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story is published; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsPublishedAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously assigns Telegram message identifiers to a story record based on its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to update.</param>
    /// <param name="messageIds">The Telegram message identifiers to set.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task SetMessageIdAsync(Guid id, TelegramMessageIds messageIds, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves a story author entity by its unique identifier including its associated message entities.
    /// </summary>
    /// <param name="id">The unique identifier of the story author to retrieve.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>The story author entity with its associated message entities.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity with the specified ID is not found.</exception>
    Task<StoryAuthor> GetWithMessagesAsync(Guid id, CancellationToken ct = default);
}