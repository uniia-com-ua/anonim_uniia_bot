using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Provides business logic implementation for managing story authors,
/// including cryptographic encryption and hashing of sensitive user identifiers.
/// </summary>
public sealed class StoryAuthorService(
    IStoryAuthorRepository storyAuthorRepository,
    ISymmetricEncryptionService symmetricEncryptionService,
    IHashService hashService)
    : IStoryAuthorService
{
    /// <inheritdoc />
    public Task<bool> ExistsAsync(long telegramId, CancellationToken ct = default)
        => storyAuthorRepository.ExistsAsync(ComputeTelegramIdHash(telegramId), ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid storyId, CancellationToken ct = default)
        => storyAuthorRepository.ExistsAsync(storyId, ct);

    /// <inheritdoc />
    public async Task<TelegramMessageIds> GetAsync(Guid id, CancellationToken ct = default)
    {
        var story = await storyAuthorRepository.GetWithMessagesAsync(id, ct);

        var mediaFiles = story.StoryMessages?
            .Where(x => !string.IsNullOrEmpty(x.FileId))
            .ToDictionary(x => x.FileId, x => x.Type);

        return new TelegramMessageIds(
            story.ChannelMessageId,
            mediaFiles is { Count: > 0 } ? mediaFiles : null);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(long telegramId, CancellationToken ct = default)
    {
        var stringId = telegramId.ToString();

        var storyAuthor = await storyAuthorRepository.CreateAsync(
            new()
            {
                AuthorId = symmetricEncryptionService.Encrypt(stringId),
                AuthorIdHash = hashService.ComputeHash(stringId),
            },
            ct);

        await storyAuthorRepository.SaveChangesAsync(ct);

        return storyAuthor.Id;
    }

    /// <inheritdoc />
    public Task SetMessageIdAsync(Guid storyId, TelegramMessageIds messageIds, CancellationToken ct = default)
        => storyAuthorRepository.SetMessageIdAsync(storyId, messageIds, ct);

    /// <inheritdoc />
    public async Task<long> GetDecryptedTelegramIdAsync(Guid id, CancellationToken ct = default)
    {
        var author = await storyAuthorRepository.GetAsync(id, ct);

        return long.Parse(symmetricEncryptionService.Decrypt(author.AuthorId));
    }

    /// <inheritdoc />
    public Task MarkAsPublishedAsync(Guid id, CancellationToken ct = default)
        => storyAuthorRepository.MarkAsPublishedAsync(id, ct);

    /// <inheritdoc />
    public Task<bool> IsPublishedAsync(Guid id, CancellationToken ct = default)
        => storyAuthorRepository.IsPublishedAsync(id, ct);

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await storyAuthorRepository.DeleteAsync(id, ct);
        await storyAuthorRepository.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task<int> GetActualStoryMessageIdAsync(long telegramId, CancellationToken ct = default)
        => storyAuthorRepository.GetActualStoryMessageIdAsync(ComputeTelegramIdHash(telegramId), ct);

    private string ComputeTelegramIdHash(long telegramId) =>
        hashService.ComputeHash(telegramId.ToString());
}