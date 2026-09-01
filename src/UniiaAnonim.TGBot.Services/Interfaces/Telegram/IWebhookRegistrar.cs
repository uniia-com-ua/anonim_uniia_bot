namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines a service responsible for managing the registration and removal
/// of the Telegram bot webhook.
/// </summary>
public interface IWebhookRegistrar
{
    /// <summary>
    /// Asynchronously registers the bot's webhook with the Telegram Bot API.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    Task RegisterWebhookAsync(CancellationToken ct);

    /// <summary>
    /// Asynchronously removes the bot's webhook from the Telegram Bot API.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    Task RemoveWebhookAsync(CancellationToken ct);
}