using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Telegram;

/// <summary>
/// A hosted service that manages the lifecycle of the Telegram bot webhook.
/// Automatically registers the webhook on application startup and removes it on graceful shutdown.
/// </summary>
/// <param name="scopeFactory">The factory used to create service scopes for resolving scoped dependencies.</param>
/// <param name="logger">The logger used to record diagnostic information and errors during webhook initialization.</param>
public class TelegramWebhookInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramWebhookInitializer> logger) : IHostedService
{
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// Creates a dependency injection scope, resolves the <see cref="IWebhookRegistrar"/>,
    /// and asynchronously registers the Telegram webhook.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A task that represents the asynchronous startup operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var registrar = scope.ServiceProvider.GetRequiredService<IWebhookRegistrar>();

        try
        {
            await registrar.RegisterWebhookAsync(cancellationToken);
            logger.LogInformation("Webhook successfully set.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set webhook.");
        }
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// Creates a dependency injection scope, resolves the <see cref="IWebhookRegistrar"/>,
    /// and asynchronously removes the Telegram webhook.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var registrar = scope.ServiceProvider.GetRequiredService<IWebhookRegistrar>();

        try
        {
            await registrar.RemoveWebhookAsync(cancellationToken);
            logger.LogInformation("Webhook successfully removed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove webhook on application shutdown.");
        }
    }
}