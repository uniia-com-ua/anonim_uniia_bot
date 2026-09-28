using Telegram.Bot.Types;

namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines a strategy for handling specific types of incoming Telegram updates.
/// </summary>
public interface ITelegramUpdateStrategy
{
    /// <summary>
    /// Asynchronously determines whether this strategy can process the provided Telegram update.
    /// </summary>
    /// <param name="update">The incoming update received from the Telegram Bot API.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>
    /// <see langword="true"/> if the strategy can handle the update; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> CanHandleAsync(Update update, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously processes the Telegram update according to the strategy's specific logic.
    /// </summary>
    /// <param name="update">The incoming update to be processed.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous handling operation.</returns>
    Task HandleAsync(Update update, CancellationToken ct = default);
}