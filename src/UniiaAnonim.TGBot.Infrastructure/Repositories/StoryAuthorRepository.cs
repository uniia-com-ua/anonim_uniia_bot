using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Infrastructure.Persistence;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

/// <summary>
/// Provides repository implementation for managing <see cref="StoryAuthor"/> entities,
/// including secure encryption handling for sensitive identifiers.
/// </summary>
/// <param name="appDbContext">The application database context.</param>
public class StoryAuthorRepository(
    AppDbContext appDbContext)
    : GenericRepository<StoryAuthor>(appDbContext),
    IStoryAuthorRepository
{
    /// <summary>
    /// Asynchronously checks whether the specified user has an active (not published) story in the database.
    /// </summary>
    /// <param name="authorIdHash">The unique hash of the author to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if an active story exists for the author; otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> HasUserActiveStoryAsync(string authorIdHash, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.AuthorIdHash == authorIdHash && x.Status != StoryStatus.Published, ct);

    /// <summary>
    /// Asynchronously checks whether an entity with the specified story identifier exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the story to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the entity exists; otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, ct);

    /// <summary>
    /// Asynchronously retrieves the actual (latest unpublished) channel message identifier for the specified author identifier.
    /// </summary>
    /// <param name="authorIdHash">The unique author identifier to search for.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the channel message identifier (<see cref="int"/>).
    /// </returns>
    /// <exception cref="EntityNotFoundException">Thrown when no unpublished story is found for the specified author.</exception>
    public async Task<int> GetActualStoryMessageIdAsync(string authorIdHash, CancellationToken ct = default)
    {
        var messageId = await DbSet
            .AsNoTracking()
            .Where(x => x.AuthorIdHash == authorIdHash && x.Status == StoryStatus.Pending)
            .Select(x => x.ChannelMessageId)
            .FirstOrDefaultAsync(ct);

        return messageId != default ? messageId : throw new EntityNotFoundException(nameof(StoryAuthor), authorIdHash);
    }

    /// <summary>
    /// Asynchronously checks if a story author record is marked as published.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story is published; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsPublishedAsync(Guid id, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.Id == id && x.Status == StoryStatus.Published, ct);

    /// <summary>
    /// Asynchronously assigns Telegram message identifiers to a story record based on its unique identifier, including related media messages.
    /// </summary>
    /// <param name="id">The unique identifier of the story record to update.</param>
    /// <param name="messageIds">The Telegram message identifiers to set.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    public async Task SetMessageIdAsync(Guid id, TelegramMessageIds messageIds, CancellationToken ct = default)
    {
        await DbSet
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.ChannelMessageId, messageIds.InteractiveMessageId),
                ct);

        if (messageIds.MediaFiles is { Count: > 0 })
        {
            var existingMediaEntities = await Context.Set<StoryFileEntity>()
                .Where(x => x.StoryId == id)
                .ToListAsync(ct);

            if (existingMediaEntities.Count > 0)
            {
                Context.Set<StoryFileEntity>().RemoveRange(existingMediaEntities);
            }

            var newMediaEntities = messageIds.MediaFiles
                .Select(pair => new StoryFileEntity
                {
                    StoryId = id,
                    FileId = pair.Key,
                    Type = pair.Value,
                });

            await Context.Set<StoryFileEntity>().AddRangeAsync(newMediaEntities, ct);
            await Context.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Asynchronously retrieves a story author entity by its unique identifier including its associated message entities.
    /// </summary>
    /// <param name="id">The unique identifier of the story author to retrieve.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>The story author entity with its associated message entities.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity with the specified ID is not found.</exception>
    public async Task<StoryAuthor> GetWithMessagesAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.StoryMessages)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(StoryAuthor), id);
    }

    /// <summary>
    /// Asynchronously checks if the story with the specified identifier is in the given status.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the story is in the specified status; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsInStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .AnyAsync(x => x.Id == storyId && x.Status == status, ct);

    /// <summary>
    /// Asynchronously checks if the story for the specified author identifier hash is in the given status.
    /// </summary>
    /// <param name="authorIdHash">The hashed identifier of the author.</param>
    /// <param name="status">The story status to check against.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the author's story is in the specified status; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsInStatusAsync(string authorIdHash, StoryStatus status, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .AnyAsync(x => x.AuthorIdHash == authorIdHash && x.Status == status, ct);

    /// <summary>
    /// Asynchronously updates the status of the story with the specified identifier.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story record.</param>
    /// <param name="status">The new story status to set.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    public async Task SetStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default)
        => await DbSet
                .Where(x => x.Id == storyId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status), ct);
}