using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Provides business logic implementation for managing story authors,
/// including cryptographic encryption and hashing of sensitive user identifiers.
/// </summary>
public sealed class StoryAuthorService(
    IStoryAuthorRepository storyAuthorRepository,
    IAuthorAgreementRepository authorAgreementRepository,
    ISymmetricEncryptionService symmetricEncryptionService,
    IHashService hashService,
    IMemoryCache memoryCache)
    : IStoryAuthorService
{
    private static readonly TimeSpan AgreementCacheDuration = TimeSpan.FromHours(24);

    /// <inheritdoc />
    public async Task<TelegramMessageIds> GetAsync(Guid id, CancellationToken ct = default)
    {
        var story = await storyAuthorRepository.GetWithMessagesAsync(id, ct);

        Dictionary<string, StoryMediaType>? mediaFiles = null;
        if (story.StoryMessages is { Count: > 0 })
        {
            mediaFiles = new Dictionary<string, StoryMediaType>(story.StoryMessages.Count);
            foreach (var message in story.StoryMessages)
            {
                if (!string.IsNullOrEmpty(message.FileId))
                {
                    mediaFiles[message.FileId] = message.Type;
                }
            }
        }

        return new TelegramMessageIds(
            story.ChannelMessageId,
            mediaFiles is { Count: > 0 } ? mediaFiles : null);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(long telegramId, CancellationToken ct = default)
    {
        var stringId = telegramId.ToString(CultureInfo.InvariantCulture);

        var storyAuthor = await storyAuthorRepository.CreateAsync(
            new()
            {
                AuthorId = symmetricEncryptionService.Encrypt(stringId),
                AuthorIdHash = hashService.ComputeHash(stringId),
                Status = StoryStatus.Pending,
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
        var encryptedAuthorId = await storyAuthorRepository.GetEncryptedAuthorIdAsync(id, ct);

        return long.Parse(symmetricEncryptionService.Decrypt(encryptedAuthorId), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => storyAuthorRepository.DeleteAsync(id, ct);

    /// <inheritdoc />
    public Task<int> GetActualStoryMessageIdAsync(long telegramId, CancellationToken ct = default)
        => storyAuthorRepository.GetActualStoryMessageIdAsync(ComputeTelegramIdHash(telegramId), ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid storyId, CancellationToken ct = default)
        => storyAuthorRepository.ExistsAsync(storyId, ct);

    /// <inheritdoc />
    public Task<bool> HasUserActiveStoryAsync(long telegramId, CancellationToken ct = default)
        => storyAuthorRepository.HasUserActiveStoryAsync(ComputeTelegramIdHash(telegramId), ct);

    /// <inheritdoc />
    public async Task<bool> HasAcceptedRulesAsync(long telegramId, CancellationToken ct = default)
    {
        var cacheKey = GetRulesCacheKey(telegramId);
        if (memoryCache.TryGetValue(cacheKey, out bool accepted) && accepted)
        {
            return true;
        }

        var hasAccepted = await authorAgreementRepository.HasAcceptedRulesAsync(ComputeTelegramIdHash(telegramId), ct);
        if (hasAccepted)
        {
            memoryCache.Set(cacheKey, true, AgreementCacheDuration);
        }

        return hasAccepted;
    }

    /// <inheritdoc />
    public async Task AcceptRulesAsync(long telegramId, CancellationToken ct = default)
    {
        await authorAgreementRepository.AcceptRulesAsync(ComputeTelegramIdHash(telegramId), ct);
        memoryCache.Set(GetRulesCacheKey(telegramId), true, AgreementCacheDuration);
    }

    /// <inheritdoc />
    public Task<bool> IsInStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default)
        => storyAuthorRepository.IsInStatusAsync(storyId, status, ct);

    /// <inheritdoc />
    public Task<bool> IsInStatusAsync(long telegramId, StoryStatus status, CancellationToken ct = default)
        => storyAuthorRepository.IsInStatusAsync(ComputeTelegramIdHash(telegramId), status, ct);

    /// <inheritdoc />
    public Task SetStatusAsync(Guid storyId, StoryStatus status, CancellationToken ct = default)
        => storyAuthorRepository.SetStatusAsync(storyId, status, ct);

    private string ComputeTelegramIdHash(long telegramId) =>
        hashService.ComputeHash(telegramId.ToString(CultureInfo.InvariantCulture));

    private static string GetRulesCacheKey(long telegramId) => $"rules_accepted_{telegramId}";
}