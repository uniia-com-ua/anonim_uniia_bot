using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

/// <summary>
/// Defines repository methods for managing story authors and querying story records.
/// </summary>
public interface IStoryAuthorRepository
    : IGenericRepository<StoryAuthor>
{
    /// <summary>
    /// Asynchronously checks whether the specified user has an active (not published) story in the database.
    /// </summary>
    /// <param name="authorIdHash">The unique hash of the author to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if an active story exists for the author; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> HasUserActiveStoryAsync(string authorIdHash, CancellationToken ct = default);

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
    /// Asynchronously checks if the story with the specified identifier is in the given status.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story is in the specified status; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks if the story for the specified author identifier hash is in the given status.
    /// </summary>
    /// <param name="authorIdHash">The hashed identifier of the author.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the author's story is in the specified status; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInStatusAsync(string authorIdHash, StoryStatus status, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously updates the status of the story with the specified identifier.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The new story status to set.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task SetStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default);

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