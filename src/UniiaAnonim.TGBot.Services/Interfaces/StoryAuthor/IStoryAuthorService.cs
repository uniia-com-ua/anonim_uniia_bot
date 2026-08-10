using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Defines methods for managing story authors and secure record creation.
/// </summary>
public interface IStoryAuthorService
{
    /// <summary>
    /// Asynchronously checks whether a story author exists by their telegram identifier.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the author exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks whether a story exists by its unique identifier.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(Guid storyId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously assigns Telegram message identifiers to an existing story author record.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="messageIds">The Telegram message identifiers associated with the delivered story.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task SetMessageIdAsync(Guid storyId, TelegramMessageIds messageIds, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously creates a new story author record with an encrypted telegram identifier.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier to be encrypted and stored.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous creation operation. The task result contains
    /// the unique identifier (<see cref="Guid"/>) of the newly created story author record.
    /// </returns>
    Task<Guid> CreateAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously marks a story author record as published based on its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to update.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task MarkAsPublishedAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously deletes a story author record and saves the changes to the database.
    /// </summary>
    /// <param name="id">The unique identifier of the story author record to delete.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if the story with the specified identifier is already published.
    /// </summary>
    /// <param name="id">The unique identifier of the story to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story is published; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsPublishedAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves the Telegram message identifiers associated with a story author record by its unique identifier.
    /// </summary>
    /// <param name="id">The internal unique identifier of the story author record.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="TelegramMessageIds"/> associated with the story.
    /// </returns>
    Task<TelegramMessageIds> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves a story author by their unique identifier and decrypts sensitive fields.
    /// </summary>
    /// <param name="id">The internal unique identifier of the story author.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the decrypted Telegram identifier.
    /// </returns>
    Task<long> GetDecryptedTelegramIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously retrieves the channel message identifier of the actual (latest unpublished) story
    /// for the specified Telegram user identifier by hashing it and searching the database.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the channel message identifier (<see cref="int"/>) of the actual unpublished story.
    /// </returns>
    Task<int> GetActualStoryMessageIdAsync(long telegramId, CancellationToken ct = default);
}