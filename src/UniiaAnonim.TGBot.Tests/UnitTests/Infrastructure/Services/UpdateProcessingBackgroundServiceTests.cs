using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Infrastructure.Services;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for the <see cref="UpdateProcessingBackgroundService"/> class.
/// </summary>
public class UpdateProcessingBackgroundServiceTests
    : IDisposable
{
    private readonly Channel<Update> _channel;
    private readonly IServiceProvider _serviceProviderSubstitute;
    private readonly IServiceScopeFactory _scopeFactorySubstitute;
    private readonly IServiceScope _serviceScopeSubstitute;
    private readonly IServiceProvider _scopedServiceProviderSubstitute;
    private readonly ITelegramUpdateDispatcher _dispatcherSubstitute;
    private readonly ILogger<UpdateProcessingBackgroundService> _loggerSubstitute;
    private readonly TestableUpdateProcessingBackgroundService _service;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProcessingBackgroundServiceTests"/> class.
    /// </summary>
    public UpdateProcessingBackgroundServiceTests()
    {
        _channel = Channel.CreateUnbounded<Update>();
        _loggerSubstitute = Substitute.For<ILogger<UpdateProcessingBackgroundService>>();
        _dispatcherSubstitute = Substitute.For<ITelegramUpdateDispatcher>();

        // Set up dependency injection scopes
        _serviceProviderSubstitute = Substitute.For<IServiceProvider>();
        _scopeFactorySubstitute = Substitute.For<IServiceScopeFactory>();
        _serviceScopeSubstitute = Substitute.For<IServiceScope>();
        _scopedServiceProviderSubstitute = Substitute.For<IServiceProvider>();

        // IServiceProvider.CreateScope() extension resolves IServiceScopeFactory under the hood
        _serviceProviderSubstitute.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactorySubstitute);
        _scopeFactorySubstitute.CreateScope().Returns(_serviceScopeSubstitute);
        _serviceScopeSubstitute.ServiceProvider.Returns(_scopedServiceProviderSubstitute);

        // GetRequiredService<ITelegramUpdateDispatcher>() resolves ITelegramUpdateDispatcher
        _scopedServiceProviderSubstitute.GetService(typeof(ITelegramUpdateDispatcher)).Returns(_dispatcherSubstitute);

        _service = new TestableUpdateProcessingBackgroundService(_channel, _serviceProviderSubstitute, _loggerSubstitute);
    }

    /// <summary>
    /// Ensures that the background service correctly reads updates from the channel,
    /// creates a scope for each, and dispatches them successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsyncWhenUpdatesAreInChannelProcessesThemSuccessfully()
    {
        // Arrange
        var update1 = new Update { Id = 1 };
        var update2 = new Update { Id = 2 };

        await _channel.Writer.WriteAsync(update1);
        await _channel.Writer.WriteAsync(update2);

        _channel.Writer.Complete();

        // Act
        await _service.RunExecuteAsync(CancellationToken.None);

        // Assert
        _scopeFactorySubstitute.Received(2).CreateScope();
        await _dispatcherSubstitute.Received(1).DispatchAsync(update1, Arg.Any<CancellationToken>());
        await _dispatcherSubstitute.Received(1).DispatchAsync(update2, Arg.Any<CancellationToken>());

        _serviceScopeSubstitute.Received(2).Dispose();
    }

    /// <summary>
    /// Ensures that if the dispatcher throws an exception during processing,
    /// the background service catches it, logs an error, and continues processing the next updates.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsyncWhenDispatcherThrowsExceptionLogsErrorAndContinuesProcessing()
    {
        // Arrange
        var update1 = new Update { Id = 100 };
        var update2 = new Update { Id = 200 };
        var expectedException = new InvalidOperationException("Something went wrong");

        await _channel.Writer.WriteAsync(update1);
        await _channel.Writer.WriteAsync(update2);
        _channel.Writer.Complete();

        _dispatcherSubstitute
            .DispatchAsync(update1, Arg.Any<CancellationToken>())
            .Throws(expectedException);

        _dispatcherSubstitute
            .DispatchAsync(update2, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.RunExecuteAsync(CancellationToken.None);

        // Assert
        await _dispatcherSubstitute.Received(1).DispatchAsync(update1, Arg.Any<CancellationToken>());
        await _dispatcherSubstitute.Received(1).DispatchAsync(update2, Arg.Any<CancellationToken>());

        _loggerSubstitute.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Ensures that triggering the cancellation token immediately stops the execution
    /// by throwing an OperationCanceledException (expected behavior for ReadAllAsync).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsyncWhenCancellationTokenIsTriggeredStopsExecution()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var update = new Update { Id = 1 };
        await _channel.Writer.WriteAsync(update);

        await cts.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => _service.RunExecuteAsync(cts.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
        await _dispatcherSubstitute.DidNotReceiveWithAnyArgs().DispatchAsync(default, default);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _serviceScopeSubstitute?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// A testable wrapper around <see cref="UpdateProcessingBackgroundService"/>
    /// to expose the protected <see cref="BackgroundService.ExecuteAsync"/> method.
    /// </summary>
    private class TestableUpdateProcessingBackgroundService(
        Channel<Update> channel,
        IServiceProvider serviceProvider,
        ILogger<UpdateProcessingBackgroundService> logger)
        : UpdateProcessingBackgroundService(channel, serviceProvider, logger)
    {
        public Task RunExecuteAsync(CancellationToken stoppingToken)
        {
            return ExecuteAsync(stoppingToken);
        }
    }
}