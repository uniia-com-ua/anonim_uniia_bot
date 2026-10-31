using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Services.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for the <see cref="BannedUserService"/> class.
/// </summary>
public class BannedUserServiceTests : IDisposable
{
    private readonly IBannedUserRepository _repositorySubstitute;
    private readonly IHashService _hashServiceSubstitute;
    private readonly MemoryCache _memoryCache;
    private readonly ILogger<BannedUserService> _loggerSubstitute;
    private readonly BannedUserService _service;
    private bool _disposed;

    public BannedUserServiceTests()
    {
        _repositorySubstitute = Substitute.For<IBannedUserRepository>();
        _hashServiceSubstitute = Substitute.For<IHashService>();
        _hashServiceSubstitute.ComputeHash(Arg.Any<string>())
            .Returns(callInfo => $"hash_{callInfo.Arg<string>()}");

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerSubstitute = Substitute.For<ILogger<BannedUserService>>();

        _service = new BannedUserService(
            _repositorySubstitute,
            _hashServiceSubstitute,
            _memoryCache,
            _loggerSubstitute);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that when a user's ban status is already cached in memory,
    /// IsBannedAsync returns true immediately without querying the database repository.
    /// </summary>
    [Fact]
    public async Task IsBannedAsyncWhenUserInCacheReturnsTrueWithoutQueryingRepository()
    {
        // Arrange
        const long telegramId = 123456789;
        _memoryCache.Set($"banned_user_{telegramId}", true, TimeSpan.FromMinutes(30));

        // Act
        var result = await _service.IsBannedAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        await _repositorySubstitute.DidNotReceiveWithAnyArgs().IsBannedAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that when a user is confirmed banned in the database on cache miss,
    /// the status is cached in memory and true is returned.
    /// </summary>
    [Fact]
    public async Task IsBannedAsyncWhenUserBannedInDbCachesAndReturnsTrue()
    {
        // Arrange
        const long telegramId = 987654321;
        var hash = $"hash_{telegramId.ToString(CultureInfo.InvariantCulture)}";
        _repositorySubstitute.IsBannedAsync(hash, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var firstCallResult = await _service.IsBannedAsync(telegramId, TestContext.Current.CancellationToken);
        var secondCallResult = await _service.IsBannedAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(firstCallResult);
        Assert.True(secondCallResult);

        // Repository should be called only once due to anti-DDoS caching
        await _repositorySubstitute.Received(1).IsBannedAsync(hash, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when a user is not banned in the database, false is returned.
    /// </summary>
    [Fact]
    public async Task IsBannedAsyncWhenUserNotBannedReturnsFalse()
    {
        // Arrange
        const long telegramId = 555555;
        var hash = $"hash_{telegramId.ToString(CultureInfo.InvariantCulture)}";
        _repositorySubstitute.IsBannedAsync(hash, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _service.IsBannedAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
        await _repositorySubstitute.Received(1).IsBannedAsync(hash, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that BanUserAsync delegates to the repository and immediately sets the in-memory cache.
    /// </summary>
    [Fact]
    public async Task BanUserAsyncPersistsToRepositoryAndSetsCache()
    {
        // Arrange
        const long telegramId = 777777;
        const string reason = "Spam";
        var hash = $"hash_{telegramId.ToString(CultureInfo.InvariantCulture)}";

        // Act
        await _service.BanUserAsync(telegramId, reason, TestContext.Current.CancellationToken);

        // Assert
        await _repositorySubstitute.Received(1).BanUserAsync(hash, reason, Arg.Any<CancellationToken>());
        Assert.True(_memoryCache.TryGetValue($"banned_user_{telegramId}", out bool isBanned));
        Assert.True(isBanned);
    }

    /// <summary>
    /// Verifies that UnbanUserAsync delegates to the repository and removes the entry from the in-memory cache.
    /// </summary>
    [Fact]
    public async Task UnbanUserAsyncCallsRepositoryAndEvictsMemoryCache()
    {
        // Arrange
        const long telegramId = 888888;
        var hash = $"hash_{telegramId.ToString(CultureInfo.InvariantCulture)}";
        _memoryCache.Set($"banned_user_{telegramId}", true, TimeSpan.FromHours(1));

        // Act
        await _service.UnbanUserAsync(telegramId, TestContext.Current.CancellationToken);

        // Assert
        await _repositorySubstitute.Received(1).UnbanUserAsync(hash, Arg.Any<CancellationToken>());
        Assert.False(_memoryCache.TryGetValue($"banned_user_{telegramId}", out _));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
            }

            _disposed = true;
        }
    }
}
