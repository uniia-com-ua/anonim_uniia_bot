using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the edit story callback query.
/// Prompts the administrator to reply with the edited text for the story.
/// </summary>
public sealed class EditStoryCallbackStrategy(
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    ILogger<EditStoryCallbackStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.StartsWith(ButtonPrefixes.AdminEditStoryPrefix, StringComparison.OrdinalIgnoreCase));
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

        var storyIdString = callbackQuery.Data[ButtonPrefixes.AdminEditStoryPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            logger.LogWarning("Invalid story ID format in callback data: {CallbackData}", callbackQuery.Data);
            return;
        }

        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: localizer["str0018", storyId].UnescapedValue(),
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}