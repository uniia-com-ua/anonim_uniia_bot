using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Infrastructure.Services;

/// <summary>
/// A background service responsible for asynchronously processing incoming Telegram updates
/// from an in-memory queue (Channel).
/// </summary>
public partial class UpdateProcessingBackgroundService(
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
                await using var scope = serviceProvider.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<ITelegramUpdateDispatcher>();

                await dispatcher.DispatchAsync(update, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogUpdateProcessingFailed(logger, ex, update.Id);
            }
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.UpdateProcessingFailed,
        Level = LogLevel.Error,
        Message = "An error occurred while processing update {UpdateId}")]
    private static partial void LogUpdateProcessingFailed(ILogger logger, Exception exception, int updateId);
}