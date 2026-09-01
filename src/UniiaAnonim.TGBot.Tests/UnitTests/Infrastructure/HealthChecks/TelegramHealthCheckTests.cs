using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Infrastructure.HealthChecks;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Infrastructure.HealthChecks;

/// <summary>
/// Unit tests for the <see cref="TelegramHealthCheck"/> class.
/// </summary>
public class TelegramHealthCheckTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly TelegramHealthCheck _healthCheck;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramHealthCheckTests"/> class.
    /// </summary>
    public TelegramHealthCheckTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _healthCheck = new TelegramHealthCheck(_botClientSubstitute);
    }

    /// <summary>
    /// Ensures that when the bot client successfully retrieves the bot's profile,
    /// the health check returns a healthy result containing the bot's username.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsyncWhenApiCallIsSuccessfulReturnsHealthyResultWithUsername()
    {
        // Arrange
        var context = new HealthCheckContext();
        var expectedUser = new User { Id = 123, FirstName = "Test", Username = "TestBot" };

        _botClientSubstitute
            .SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Connected to bot: TestBot", result.Description);
        Assert.Null(result.Exception);

        await _botClientSubstitute.Received(1).SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that when the bot client throws an exception, the health check
    /// catches it and returns an unhealthy result with the exception attached.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsyncWhenApiCallFailsReturnsUnhealthyResultWithException()
    {
        // Arrange
        var context = new HealthCheckContext();
        var expectedException = new InvalidOperationException("API request failed");

        _botClientSubstitute
            .SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Telegram API is unreachable", result.Description);
        Assert.Same(expectedException, result.Exception);

        await _botClientSubstitute.Received(1).SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that if the retrieved bot profile has a null username (edge case),
    /// the health check handles the string interpolation gracefully without throwing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsyncWhenUsernameIsNullReturnsHealthyResultGracefully()
    {
        // Arrange
        var context = new HealthCheckContext();
        var expectedUser = new User { Id = 123, FirstName = "NamelessBot", Username = null };

        _botClientSubstitute
            .SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Connected to bot: ", result.Description);

        await _botClientSubstitute.Received(1).SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that the health check executes successfully and returns the correct status
    /// even if a null context is provided, as the current implementation does not rely on it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsyncWhenContextIsNullExecutesSuccessfully()
    {
        // Arrange
        HealthCheckContext? nullContext = null;
        var expectedUser = new User { Id = 123, FirstName = "Test", Username = "TestBot" };

        _botClientSubstitute
            .SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        // Act
        var result = await _healthCheck.CheckHealthAsync(nullContext!, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    /// <summary>
    /// Ensures that triggering the cancellation token results in an OperationCanceledException
    /// being thrown by the client, which is then correctly caught and wrapped in an Unhealthy result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsyncWhenCancellationTokenIsTriggeredCatchesCancellationAndReturnsUnhealthy()
    {
        // Arrange
        var context = new HealthCheckContext();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cancellationException = new OperationCanceledException("The operation was canceled.");

        _botClientSubstitute
            .SendRequest(Arg.Any<GetMeRequest>(), cts.Token)
            .ThrowsAsync(cancellationException);

        // Act
        var result = await _healthCheck.CheckHealthAsync(context, cts.Token);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Telegram API is unreachable", result.Description);
        Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);

        await _botClientSubstitute.Received(1).SendRequest(Arg.Any<GetMeRequest>(), cts.Token);
    }
}