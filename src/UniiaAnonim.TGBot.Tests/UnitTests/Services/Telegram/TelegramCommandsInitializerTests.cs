using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Infrastructure.Services.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for the <see cref="TelegramCommandsInitializer"/> class.
/// </summary>
public class TelegramCommandsInitializerTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly ILogger<TelegramCommandsInitializer> _loggerSubstitute;
    private readonly IOptions<GeneralOptions> _optionsSubstitute;
    private readonly TelegramCommandsInitializer _initializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramCommandsInitializerTests"/> class.
    /// </summary>
    public TelegramCommandsInitializerTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _loggerSubstitute = Substitute.For<ILogger<TelegramCommandsInitializer>>();
        _optionsSubstitute = Substitute.For<IOptions<GeneralOptions>>();
        _optionsSubstitute.Value.Returns(new GeneralOptions { DefaultLanguage = "uk-UA" });

        _localizerSubstitute[Arg.Any<string>()]
            .Returns(callInfo => new LocalizedString(callInfo.Arg<string>()!, $"{callInfo.Arg<string>()}_Translated"));

        _initializer = new TelegramCommandsInitializer(_botClientSubstitute, _localizerSubstitute, _optionsSubstitute, _loggerSubstitute);
    }

    /// <summary>
    /// Ensures that starting the service successfully sets the localized global bot commands for both group and private chats.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenCalledSetsGlobalBotCommandsSuccessfully()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _initializer.StartAsync(cancellationToken));

        // Assert
        Assert.Null(exception);

        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(req =>
                req.Scope is BotCommandScopeAllGroupChats &&
                req.Commands != null &&
                (!req.Commands.Any() || req.Commands.All(c => c.Description.EndsWith("_Translated", StringComparison.InvariantCulture)))),
            cancellationToken);

        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(req =>
                req.Scope is BotCommandScopeAllPrivateChats &&
                req.Commands != null &&
                (!req.Commands.Any() || req.Commands.All(c => c.Description.EndsWith("_Translated", StringComparison.InvariantCulture)))),
            cancellationToken);
    }

    /// <summary>
    /// Ensures that starting the service catches an exception if setting the group chats commands fails, and skips setting private chats commands.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenSetGroupChatsCommandsThrowsExceptionCatchesExceptionAndSkipsPrivateChats()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        _botClientSubstitute.SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllGroupChats),
            cancellationToken)
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _initializer.StartAsync(cancellationToken));

        // Assert
        Assert.Null(exception);

        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllGroupChats),
            cancellationToken);

        await _botClientSubstitute.DidNotReceive().SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllPrivateChats),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that starting the service catches an exception if setting the private chats commands fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenSetPrivateChatsCommandsThrowsExceptionCatchesException()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        _botClientSubstitute.SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllPrivateChats),
            cancellationToken)
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _initializer.StartAsync(cancellationToken));

        // Assert
        Assert.Null(exception);

        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllGroupChats),
            cancellationToken);

        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(req => req.Scope is BotCommandScopeAllPrivateChats),
            cancellationToken);
    }

    /// <summary>
    /// Ensures that starting the service catches a TaskCanceledException when the cancellation token is already canceled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenCancellationTokenIsCanceledCatchesException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var cancellationToken = cancellationTokenSource.Token;

        _botClientSubstitute.SendRequest(
            Arg.Any<SetMyCommandsRequest>(),
            cancellationToken)
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _initializer.StartAsync(cancellationToken));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Ensures that starting the service throws an ArgumentNullException if the logger is null,
    /// since the extension method LogInformation throws it before entering the try-catch block.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenLoggerIsNullThrowsArgumentNullException()
    {
        // Arrange
        var initializer = new TelegramCommandsInitializer(_botClientSubstitute, _localizerSubstitute, _optionsSubstitute, null);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            initializer.StartAsync(CancellationToken.None));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    /// <summary>
    /// Ensures that starting the service catches a NullReferenceException if the bot client is null,
    /// since it is accessed inside the try-catch block.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenBotClientIsNullCatchesException()
    {
        // Arrange
        var initializer = new TelegramCommandsInitializer(null, _localizerSubstitute, _optionsSubstitute, _loggerSubstitute);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            initializer.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Ensures that starting the service catches a NullReferenceException if the localizer is null,
    /// since it is accessed inside the LINQ projection within the try-catch block.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenLocalizerIsNullCatchesException()
    {
        // Arrange
        var initializer = new TelegramCommandsInitializer(_botClientSubstitute, null, _optionsSubstitute, _loggerSubstitute);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            initializer.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Ensures that stopping the service with a default cancellation token completes successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsyncWhenCalledWithDefaultTokenReturnsCompletedTask()
    {
        // Arrange
        var cancellationToken = default(CancellationToken);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _initializer.StopAsync(cancellationToken));

        // Assert
        Assert.Null(exception);
    }
}