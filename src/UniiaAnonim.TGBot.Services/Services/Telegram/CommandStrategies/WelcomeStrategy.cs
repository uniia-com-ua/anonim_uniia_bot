using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.CommandStrategies;

/// <summary>
/// Defines a welcome strategy for handling the start command.
/// Sends a standard localized welcome message with a Web App launch button to the chat.
/// </summary>
public sealed class WelcomeStrategy(
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    IOptions<TelegramBotOptions> options) : ITelegramUpdateStrategy
{
    private const string Command = "/start";
    private readonly TelegramBotOptions _options = options.Value;

    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(update is { Type: UpdateType.Message, Message.Text: { } text }
            && text.StartsWith(Command, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update.Message is null)
        {
            return;
        }

        InlineKeyboardMarkup? replyMarkup = null;

        if (!string.IsNullOrWhiteSpace(_options.AnonimWebformUrl))
        {
            replyMarkup = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithWebApp(
                    text: localizer["str0001"],
                    webApp: new WebAppInfo { Url = _options.AnonimWebformUrl }));
        }

        await botClient.SendMessage(
            chatId: update.Message.Chat.Id,
            text: localizer["str0002"],
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: ct);
    }
}