using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UniiaAnonim.TGBot.Application.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ITelegramBotClient"/> to simplify common messaging operations.
/// </summary>
public static class TelegramBotClientExtensions
{
    /// <summary>
    /// Safely edits either the message text or caption, falling back to editing the caption
    /// when the target message contains media instead of plain text.
    /// </summary>
    /// <param name="botClient">The Telegram bot client instance.</param>
    /// <param name="chatId">The unique identifier for the target chat.</param>
    /// <param name="messageId">The identifier of the message to edit.</param>
    /// <param name="text">The new text or caption content to set.</param>
    /// <param name="replyMarkup">An optional inline keyboard markup to attach.</param>
    /// <param name="parseMode">The parse mode to use. Defaults to <see cref="ParseMode.Html"/>.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous edit operation.</returns>
    public static async Task SafeEditMessageOrCaptionAsync(
        this ITelegramBotClient botClient,
        long chatId,
        int messageId,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(botClient);

        try
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                parseMode: parseMode,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }
        catch (ApiRequestException)
        {
            await botClient.EditMessageCaption(
                chatId: chatId,
                messageId: messageId,
                caption: text,
                parseMode: parseMode,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }
    }
}
