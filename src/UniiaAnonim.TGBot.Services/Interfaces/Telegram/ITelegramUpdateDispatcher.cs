using Telegram.Bot.Types;

namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines a dispatcher responsible for routing incoming Telegram updates
/// to their appropriate handlers based on the update type and content.
/// </summary>
public interface ITelegramUpdateDispatcher
{
    /// <summary>
    /// Asynchronously dispatches the incoming Telegram update to the corresponding handler.
    /// </summary>
    /// <param name="update">The incoming update received from the Telegram Bot API.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous dispatch operation.</returns>
    Task DispatchAsync(Update update, CancellationToken ct = default);
}