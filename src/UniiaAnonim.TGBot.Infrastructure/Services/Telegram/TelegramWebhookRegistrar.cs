using Microsoft.Extensions.Options;
using Telegram.Bot;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Telegram;

/// <summary>
/// Implements the <see cref="IWebhookRegistrar"/> interface to manage the registration
/// and removal of the Telegram bot's webhook.
/// </summary>
/// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
/// <param name="generalOptions">The application's general configuration, providing the base URL for the webhook.</param>
/// <param name="telegramOptions">The configured Telegram options containing the secret token for webhook validation.</param>
public class TelegramWebhookRegistrar(
    ITelegramBotClient botClient,
    IOptions<GeneralOptions> generalOptions,
    IOptions<TelegramBotOptions> telegramOptions) : IWebhookRegistrar
{
    /// <summary>
    /// Asynchronously registers the bot's webhook URL with the Telegram Bot API.
    /// Constructs the full webhook endpoint using the <see cref="GeneralOptions.BaseUrl"/>
    /// and secures it with the <see cref="TelegramBotOptions.SecretToken"/>.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the 'BaseUrl' from <see cref="GeneralOptions"/> or 'SecretToken'
    /// from <see cref="TelegramBotOptions"/> is missing.
    /// </exception>
    public async Task RegisterWebhookAsync(CancellationToken ct)
    {
        var baseUrl = generalOptions.Value.BaseUrl
            ?? throw new InvalidOperationException("BaseUrl is missing.");

        var fullUrl = $"{baseUrl.TrimEnd('/')}/{Routes.TelegramWebhook}";

        var secretToken = telegramOptions.Value.SecretToken
            ?? throw new InvalidOperationException("SecretToken is missing.");

        await botClient.SetWebhook(url: fullUrl, secretToken: secretToken, cancellationToken: ct);
    }

    /// <summary>
    /// Asynchronously removes the current webhook configuration from the Telegram Bot API.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    public async Task RemoveWebhookAsync(CancellationToken ct)
    {
        await botClient.DeleteWebhook(cancellationToken: ct);
    }
}