using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the accept rules callback query.
/// Marks that the user has accepted the rules and notifies them.
/// </summary>
public sealed class AcceptRulesCallbackStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IStringLocalizer<Messages> localizer,
    ILogger<AcceptRulesCallbackStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.Equals(ButtonPrefixes.AcceptRulesPrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var callbackQuery = update.CallbackQuery;

        if (callbackQuery.Message is null)
        {
            logger.LogWarning("Callback query {CallbackQueryId} is missing message context.", callbackQuery.Id);
            return;
        }

        var telegramId = callbackQuery.From.Id;

        await storyAuthorService.AcceptRulesAsync(telegramId, ct);

        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: localizer["str0033"].UnescapedValue(),
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}