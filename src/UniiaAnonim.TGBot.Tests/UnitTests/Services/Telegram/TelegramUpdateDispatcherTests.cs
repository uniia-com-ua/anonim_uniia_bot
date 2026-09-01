using System.Globalization;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Application.Services.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for the <see cref="TelegramUpdateDispatcher"/> class.
/// </summary>
public class TelegramUpdateDispatcherTests
{
    private readonly IDefaultTelegramUpdateStrategy _defaultStrategySubstitute;
    private readonly IOptions<GeneralOptions> _optionsSubstitute;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramUpdateDispatcherTests"/> class.
    /// Sets up common test dependencies and options.
    /// </summary>
    public TelegramUpdateDispatcherTests()
    {
        _defaultStrategySubstitute = Substitute.For<IDefaultTelegramUpdateStrategy>();
        _optionsSubstitute = Options.Create(new GeneralOptions { DefaultLanguage = "en" });
    }

    /// <summary>
    /// Verifies that the dispatcher correctly sets the execution culture using configured default language
    /// and executes the matching strategy.
    /// </summary>
    [Fact]
    public async Task DispatchAsyncWhenStrategyCanHandleSetsCultureAndExecutesStrategy()
    {
        // Arrange
        const string expectedCulture = "en";
        var update = new Update();

        var strategy = Substitute.For<ITelegramUpdateStrategy>();
        strategy.CanHandleAsync(update, Arg.Any<CancellationToken>()).Returns(true);

        string? executedCulture = null;
        strategy.HandleAsync(update, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => executedCulture = CultureInfo.CurrentCulture.Name);

        var dispatcher = new TelegramUpdateDispatcher(_optionsSubstitute, [strategy], _defaultStrategySubstitute);

        // Act
        await dispatcher.DispatchAsync(update, CancellationToken.None);

        // Assert
        await strategy.Received(1).HandleAsync(update, CancellationToken.None);
        await _defaultStrategySubstitute.DidNotReceiveWithAnyArgs().HandleAsync(default, default);
        Assert.Equal(expectedCulture, executedCulture);
    }

    /// <summary>
    /// Verifies that the dispatcher falls back to the default strategy when no registered strategies can handle the update.
    /// </summary>
    [Fact]
    public async Task DispatchAsyncWhenNoStrategyCanHandleExecutesDefaultStrategy()
    {
        // Arrange
        var update = new Update();

        var strategy1 = Substitute.For<ITelegramUpdateStrategy>();
        strategy1.CanHandleAsync(update, Arg.Any<CancellationToken>()).Returns(false);

        var dispatcher = new TelegramUpdateDispatcher(_optionsSubstitute, [strategy1], _defaultStrategySubstitute);

        // Act
        await dispatcher.DispatchAsync(update, CancellationToken.None);

        // Assert
        await strategy1.DidNotReceiveWithAnyArgs().HandleAsync(default, default);
        await _defaultStrategySubstitute.Received(1).HandleAsync(update, CancellationToken.None);
    }

    /// <summary>
    /// Verifies that when multiple strategies can handle an update, only the first matching strategy is executed.
    /// </summary>
    [Fact]
    public async Task DispatchAsyncWhenMultipleStrategiesCanHandleExecutesOnlyTheFirstOne()
    {
        // Arrange
        var update = new Update();

        var firstValidStrategy = Substitute.For<ITelegramUpdateStrategy>();
        firstValidStrategy.CanHandleAsync(update, Arg.Any<CancellationToken>()).Returns(true);

        var secondValidStrategy = Substitute.For<ITelegramUpdateStrategy>();
        secondValidStrategy.CanHandleAsync(update, Arg.Any<CancellationToken>()).Returns(true);

        var dispatcher = new TelegramUpdateDispatcher(_optionsSubstitute, [firstValidStrategy, secondValidStrategy], _defaultStrategySubstitute);

        // Act
        await dispatcher.DispatchAsync(update, CancellationToken.None);

        // Assert
        await firstValidStrategy.Received(1).HandleAsync(update, CancellationToken.None);
        await secondValidStrategy.DidNotReceiveWithAnyArgs().HandleAsync(default, default);
        await _defaultStrategySubstitute.DidNotReceiveWithAnyArgs().HandleAsync(default, default);
    }

    /// <summary>
    /// Ensures that the dispatcher applies the culture specified in options configuration.
    /// </summary>
    [Fact]
    public async Task DispatchAsyncUsesCustomConfiguredDefaultLanguageCulture()
    {
        // Arrange
        const string customCulture = "uk";
        var customOptionsSubstitute = Options.Create(new GeneralOptions { DefaultLanguage = customCulture });
        var update = new Update();

        var strategy = Substitute.For<ITelegramUpdateStrategy>();
        strategy.CanHandleAsync(update, Arg.Any<CancellationToken>()).Returns(true);

        string? executedCulture = null;
        strategy.HandleAsync(update, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => executedCulture = CultureInfo.CurrentCulture.Name);

        var dispatcher = new TelegramUpdateDispatcher(customOptionsSubstitute, [strategy], _defaultStrategySubstitute);

        // Act
        await dispatcher.DispatchAsync(update, CancellationToken.None);

        // Assert
        Assert.Equal(customCulture, executedCulture);
    }
}