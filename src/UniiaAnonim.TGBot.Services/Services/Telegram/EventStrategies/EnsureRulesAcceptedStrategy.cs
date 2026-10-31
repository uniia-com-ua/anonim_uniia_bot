using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy that intercepts incoming private messages and ensures
/// the user has accepted the rules before proceeding. If not, prompts them to accept.
/// </summary>
public sealed partial class EnsureRulesAcceptedStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IStringLocalizer<Messages> localizer,
    ILogger<EnsureRulesAcceptedStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public async Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        var msg = update.Message;

        return msg is not null &&
               msg.Chat.Type == ChatType.Private &&
               msg.From is { IsBot: false } &&
               !await storyAuthorService.HasAcceptedRulesAsync(msg.From.Id, ct);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update.Message is not { Chat: not null } message)
        {
            LogUpdateMissingMessageChatContext(logger, update.Id);
            return;
        }

        var inlineKeyboard = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(localizer["str0032"], ButtonPrefixes.AcceptRulesPrefix)]
        ]);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: localizer["str0034"].UnescapedValue(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    [LoggerMessage(
        EventId = LogEventIds.UpdateMissingMessageChatContext,
        Level = LogLevel.Warning,
        Message = "Update {UpdateId} is missing message chat context.")]
    private static partial void LogUpdateMissingMessageChatContext(ILogger logger, int updateId);
}