using Telegram.Bot.Types;

namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines a fallback strategy for handling incoming Telegram updates
/// when no other specific strategy is able to process them.
/// </summary>
public interface IDefaultTelegramUpdateStrategy
{
    /// <summary>
    /// Asynchronously processes the unhandled Telegram update.
    /// </summary>
    /// <param name="update">The incoming update to be processed.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous handling operation.</returns>
    Task HandleAsync(Update update, CancellationToken ct = default);
}