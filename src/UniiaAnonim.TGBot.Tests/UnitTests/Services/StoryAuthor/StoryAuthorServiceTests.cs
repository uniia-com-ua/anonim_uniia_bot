using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Services.StoryAuthor;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.StoryAuthor;

/// <summary>
/// Unit tests for <see cref="StoryAuthorService"/>.
/// </summary>
public class StoryAuthorServiceTests : IDisposable
{
    private readonly IStoryAuthorRepository _storyAuthorRepository;
    private readonly IAuthorAgreementRepository _authorAgreementRepository;
    private readonly ISymmetricEncryptionService _symmetricEncryptionService;
    private readonly IHashService _hashService;
    private readonly MemoryCache _memoryCache;
    private readonly StoryAuthorService _service;

    public StoryAuthorServiceTests()
    {
        _storyAuthorRepository = Substitute.For<IStoryAuthorRepository>();
        _authorAgreementRepository = Substitute.For<IAuthorAgreementRepository>();
        _symmetricEncryptionService = Substitute.For<ISymmetricEncryptionService>();
        _hashService = Substitute.For<IHashService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _service = new StoryAuthorService(
            _storyAuthorRepository,
            _authorAgreementRepository,
            _symmetricEncryptionService,
            _hashService,
            _memoryCache);
    }

    [Fact]
    public async Task CreateAsyncEncryptsTelegramIdComputesHashAndSavesEntity()
    {
        // Arrange
        const long telegramId = 123456789;
        const string encrypted = "enc_123456789";
        const string hash = "hash_123456789";
        var generatedId = Guid.NewGuid();

        _symmetricEncryptionService.Encrypt("123456789").Returns(encrypted);
        _hashService.ComputeHash("123456789").Returns(hash);
        _storyAuthorRepository.CreateAsync(Arg.Any<Domain.Models.StoryAuthor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var author = callInfo.Arg<Domain.Models.StoryAuthor>();
                typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!
                    .SetValue(author, generatedId);
                return author;
            });

        // Act
        var result = await _service.CreateAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(generatedId, result);
        await _storyAuthorRepository.Received(1).CreateAsync(
            Arg.Is<Domain.Models.StoryAuthor>(a =>
                a.AuthorId == encrypted &&
                a.AuthorIdHash == hash &&
                a.Status == StoryStatus.Pending),
            Arg.Any<CancellationToken>());
        await _storyAuthorRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDecryptedTelegramIdAsyncUsesOptimizedProjectionAndDecrypts()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        const string encrypted = "enc_987654";
        const long expectedTelegramId = 987654;

        _storyAuthorRepository.GetEncryptedAuthorIdAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(encrypted);
        _symmetricEncryptionService.Decrypt(encrypted)
            .Returns("987654");

        // Act
        var result = await _service.GetDecryptedTelegramIdAsync(storyId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedTelegramId, result);
        await _storyAuthorRepository.Received(1).GetEncryptedAuthorIdAsync(storyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsyncReturnsTelegramMessageIdsWithMediaFiles()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        var author = new Domain.Models.StoryAuthor
        {
            ChannelMessageId = 42,
            StoryMessages =
            [
                new StoryFileEntity { FileId = "photo_1", Type = StoryMediaType.Photo },
                new StoryFileEntity { FileId = "video_1", Type = StoryMediaType.Video },
            ],
        };

        _storyAuthorRepository.GetWithMessagesAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(author);

        // Act
        var result = await _service.GetAsync(storyId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(42, result.InteractiveMessageId);
        Assert.NotNull(result.MediaFiles);
        Assert.Equal(2, result.MediaFiles.Count);
        Assert.Equal(StoryMediaType.Photo, result.MediaFiles["photo_1"]);
        Assert.Equal(StoryMediaType.Video, result.MediaFiles["video_1"]);
    }

    [Fact]
    public async Task HasAcceptedRulesAsyncWhenCachedReturnsTrueWithoutDbCall()
    {
        // Arrange
        const long telegramId = 555;
        _memoryCache.Set($"rules_accepted_{telegramId}", true);

        // Act
        var result = await _service.HasAcceptedRulesAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        await _authorAgreementRepository.DidNotReceiveWithAnyArgs().HasAcceptedRulesAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasAcceptedRulesAsyncWhenNotInCacheQueriesRepositoryAndCaches()
    {
        // Arrange
        const long telegramId = 777;
        const string hash = "hash_777";
        _hashService.ComputeHash("777").Returns(hash);
        _authorAgreementRepository.HasAcceptedRulesAsync(hash, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.HasAcceptedRulesAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        await _authorAgreementRepository.Received(1).HasAcceptedRulesAsync(hash, Arg.Any<CancellationToken>());
        Assert.True(_memoryCache.TryGetValue($"rules_accepted_{telegramId}", out bool cached) && cached);
    }

    [Fact]
    public async Task AcceptRulesAsyncUpdatesRepositoryAndPopulatesCache()
    {
        // Arrange
        const long telegramId = 888;
        const string hash = "hash_888";
        _hashService.ComputeHash("888").Returns(hash);

        // Act
        await _service.AcceptRulesAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        await _authorAgreementRepository.Received(1).AcceptRulesAsync(hash, Arg.Any<CancellationToken>());
        Assert.True(_memoryCache.TryGetValue($"rules_accepted_{telegramId}", out bool cached) && cached);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
