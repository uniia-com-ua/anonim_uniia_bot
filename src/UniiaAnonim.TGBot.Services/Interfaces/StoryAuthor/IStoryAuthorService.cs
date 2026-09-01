using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Defines methods for managing story authors and secure record creation.
/// </summary>
public interface IStoryAuthorService
{
    /// <summary>
    /// Asynchronously checks whether a story exists by its unique identifier.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(Guid storyId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks if the story with the specified identifier is in the given status.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <returns><see langword="true"/> if the story is in status; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks if the story for the specified Telegram user identifier is in the given status.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the user's story is in the specified status; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInStatusAsync(long telegramId, StoryStatus status, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks whether the specified Telegram user has an active (not published) story in the database.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if an active story exists for the user; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> HasUserActiveStoryAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously checks whether the specified Telegram user has accepted the rules.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the user has accepted the rules; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> HasAcceptedRulesAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously records that the specified Telegram user has accepted the rules.
    /// </summary>
    /// <param name="telegramId">The raw Telegram user identifier.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
    Task AcceptRulesAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously updates the status of the story with the specified identifier.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The new story status to set.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task SetStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default);

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
    /// Asynchronously deletes a story author record and saves the changes to the database.
    /// </summary>
    /// <param name="id">The unique identifier of the story author record to delete.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

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