using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;

namespace UniiaAnonim.TGBot.Infrastructure.Services;

/// <summary>
/// A background service responsible for asynchronously processing incoming Telegram updates
/// from an in-memory queue (Channel).
/// </summary>
public class UpdateProcessingBackgroundService(
    Channel<Update> channel,
    IServiceProvider serviceProvider,
    ILogger<UpdateProcessingBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var update in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<ITelegramUpdateDispatcher>();

                await dispatcher.DispatchAsync(update, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing update {UpdateId}", update.Id);
            }
        }
    }
}