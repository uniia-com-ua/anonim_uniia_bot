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
/// Defines a strategy that intercepts incoming messages or actions and ensures
/// the user has accepted the rules before proceeding. If not, prompts them to accept.
/// </summary>
public sealed class EnsureRulesAcceptedStrategy(
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
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;

        if (chatId is null)
        {
            logger.LogWarning("Update {UpdateId} is missing chat context.", update.Id);
            return;
        }

        var inlineKeyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData(localizer["str0032"], ButtonPrefixes.AcceptRulesPrefix));

        await botClient.SendMessage(
            chatId: chatId.Value,
            text: localizer["str0034"].UnescapedValue(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);

        if (update.CallbackQuery is not null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
        }
    }
}