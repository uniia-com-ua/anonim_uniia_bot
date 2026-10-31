using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Telegram;

/// <summary>
/// A hosted service that manages the lifecycle of the Telegram bot webhook.
/// Automatically registers the webhook on application startup and removes it on graceful shutdown.
/// </summary>
/// <param name="scopeFactory">The factory used to create service scopes for resolving scoped dependencies.</param>
/// <param name="logger">The logger used to record diagnostic information and errors during webhook initialization.</param>
public sealed partial class TelegramWebhookInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramWebhookInitializer> logger) : IHostedService
{
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// Creates an asynchronous dependency injection scope, resolves the <see cref="IWebhookRegistrar"/>,
    /// and asynchronously registers the Telegram webhook.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A task that represents the asynchronous startup operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var registrar = scope.ServiceProvider.GetRequiredService<IWebhookRegistrar>();

        try
        {
            await registrar.RegisterWebhookAsync(cancellationToken);
            LogWebhookSuccessfullySet(logger);
        }
        catch (Exception ex)
        {
            LogFailedToSetWebhook(logger, ex);
        }
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// Creates an asynchronous dependency injection scope, resolves the <see cref="IWebhookRegistrar"/>,
    /// and asynchronously removes the Telegram webhook.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var registrar = scope.ServiceProvider.GetRequiredService<IWebhookRegistrar>();

        try
        {
            await registrar.RemoveWebhookAsync(cancellationToken);
            LogWebhookSuccessfullyRemoved(logger);
        }
        catch (Exception ex)
        {
            LogFailedToRemoveWebhook(logger, ex);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.WebhookSuccessfullySet,
        Level = LogLevel.Information,
        Message = "Webhook successfully set.")]
    private static partial void LogWebhookSuccessfullySet(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.FailedToSetWebhook,
        Level = LogLevel.Error,
        Message = "Failed to set webhook.")]
    private static partial void LogFailedToSetWebhook(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.WebhookSuccessfullyRemoved,
        Level = LogLevel.Information,
        Message = "Webhook successfully removed.")]
    private static partial void LogWebhookSuccessfullyRemoved(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.FailedToRemoveWebhook,
        Level = LogLevel.Error,
        Message = "Failed to remove webhook on application shutdown.")]
    private static partial void LogFailedToRemoveWebhook(ILogger logger, Exception exception);
}