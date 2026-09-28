using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace UniiaAnonim.TGBot.Infrastructure.HealthChecks;

/// <summary>
/// Provides a health check implementation to verify connectivity and availability
/// of the Telegram Bot API integration.
/// </summary>
/// <param name="botClient">The Telegram bot client used to test the connection to the API.</param>
public class TelegramHealthCheck(ITelegramBotClient botClient) : IHealthCheck
{
    /// <summary>
    /// Asynchronously evaluates the health status of the Telegram API connection.
    /// Attempts to retrieve the bot's own profile information to confirm connectivity.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the health check operation.</param>
    /// <returns>
    /// A task that represents the asynchronous health check operation.
    /// The task result contains a <see cref="HealthCheckResult"/> indicating either a healthy status with the bot's username,
    /// or an unhealthy status with the underlying exception if the API is unreachable.
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var me = await botClient.GetMe(cancellationToken);
            return HealthCheckResult.Healthy($"Connected to bot: {me.Username}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Telegram API is unreachable", ex);
        }
    }
}