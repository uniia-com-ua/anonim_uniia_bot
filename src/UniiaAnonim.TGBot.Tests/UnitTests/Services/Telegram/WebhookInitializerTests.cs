using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Infrastructure.Services.Telegram;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for the <see cref="TelegramWebhookInitializer"/> class.
/// </summary>
public class WebhookInitializerTests
{
    private readonly IServiceScopeFactory _scopeFactorySubstitute;
    private readonly IServiceScope _serviceScopeSubstitute;
    private readonly IServiceProvider _serviceProviderSubstitute;
    private readonly IWebhookRegistrar _registrarSubstitute;
    private readonly ILogger<TelegramWebhookInitializer> _loggerSubstitute;

    private readonly TelegramWebhookInitializer _initializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookInitializerTests"/> class.
    /// </summary>
    public WebhookInitializerTests()
    {
        _scopeFactorySubstitute = Substitute.For<IServiceScopeFactory>();
        _serviceScopeSubstitute = Substitute.For<IServiceScope>();
        _serviceProviderSubstitute = Substitute.For<IServiceProvider>();
        _registrarSubstitute = Substitute.For<IWebhookRegistrar>();
        _loggerSubstitute = Substitute.For<ILogger<TelegramWebhookInitializer>>();

        _scopeFactorySubstitute.CreateScope().Returns(_serviceScopeSubstitute);
        _serviceScopeSubstitute.ServiceProvider.Returns(_serviceProviderSubstitute);
        _serviceProviderSubstitute.GetService(typeof(IWebhookRegistrar)).Returns(_registrarSubstitute);

        _initializer = new TelegramWebhookInitializer(_scopeFactorySubstitute, _loggerSubstitute);
    }

    /// <summary>
    /// Ensures that the StartAsync method successfully registers the webhook and logs information.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenRegistrationSucceedsLogsInformation()
    {
        // Arrange
        _registrarSubstitute
            .RegisterWebhookAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _initializer.StartAsync(CancellationToken.None);

        // Assert
        _scopeFactorySubstitute.Received(1).CreateScope();
        await _registrarSubstitute.Received(1).RegisterWebhookAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that if the registration throws an exception, the StartAsync method catches it and logs an error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncWhenRegistrationThrowsCatchesExceptionAndLogsError()
    {
        // Arrange
        var expectedException = new InvalidOperationException("API down");
        _registrarSubstitute
            .RegisterWebhookAsync(Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        await _initializer.StartAsync(CancellationToken.None);

        // Assert
        _scopeFactorySubstitute.Received(1).CreateScope();
        await _registrarSubstitute.Received(1).RegisterWebhookAsync(Arg.Any<CancellationToken>());

        _loggerSubstitute.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Failed to set webhook.")),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Ensures that the StopAsync method successfully removes the webhook and logs information.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsyncWhenRemovalSucceedsLogsInformation()
    {
        // Arrange
        _registrarSubstitute
            .RemoveWebhookAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _initializer.StopAsync(CancellationToken.None);

        // Assert
        _scopeFactorySubstitute.Received(1).CreateScope();
        await _registrarSubstitute.Received(1).RemoveWebhookAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures that if the removal throws an exception, the StopAsync method catches it and logs an error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsyncWhenRemovalThrowsCatchesExceptionAndLogsError()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Network failure");
        _registrarSubstitute
            .RemoveWebhookAsync(Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        await _initializer.StopAsync(CancellationToken.None);

        // Assert
        _scopeFactorySubstitute.Received(1).CreateScope();
        await _registrarSubstitute.Received(1).RemoveWebhookAsync(Arg.Any<CancellationToken>());

        _loggerSubstitute.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Failed to remove webhook on application shutdown.")),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }
}