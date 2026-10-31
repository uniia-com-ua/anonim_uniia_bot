using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Extensions;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for unbanning a user when an administrator clicks the unban button on a previously banned story.
/// Removes the ban from the database and memory cache, and restores the standard moderation buttons under the story.
/// </summary>
public sealed partial class UnbanUserStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IBannedUserService bannedUserService,
    IAdminActionKeyboardFactory keyboardFactory,
    IStringLocalizer<Messages> localizer,
    ILogger<UnbanUserStrategy> logger)
    : ITelegramUpdateStrategy
{
    private const string StatusSeparator = "━━━━━━━━━━━━━━━";

    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.StartsWith(ButtonPrefixes.AdminUnbanUserPrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var callbackQuery = update.CallbackQuery;
        var message = callbackQuery.Message;

        if (message is null)
        {
            LogCallbackQueryMissingMessageContext(logger, callbackQuery.Id);
            return;
        }

        var storyIdString = callbackQuery.Data[ButtonPrefixes.AdminUnbanUserPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            LogInvalidStoryIdFormat(logger, callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);

            await bannedUserService.UnbanUserAsync(authorTelegramId, ct);

            var moderationKeyboard = keyboardFactory.CreateModerationKeyboard(storyId);

            var currentText = message.Text ?? message.Caption;
            if (!string.IsNullOrEmpty(currentText))
            {
                var separatorIndex = currentText.IndexOf(StatusSeparator, StringComparison.Ordinal);
                var restoredText = separatorIndex >= 0 ? currentText[..separatorIndex].TrimEnd() : currentText;

                await botClient.SafeEditMessageOrCaptionAsync(
                    message.Chat.Id,
                    message.MessageId,
                    restoredText.EscapeTelegramHtml(),
                    replyMarkup: moderationKeyboard,
                    ct: ct);
            }
            else
            {
                await botClient.EditMessageReplyMarkup(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    replyMarkup: moderationKeyboard,
                    cancellationToken: ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0042"],
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            LogUnbanUserFailed(logger, ex, storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0043"], showAlert: true, cancellationToken: ct);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.CallbackQueryMissingMessageContext,
        Level = LogLevel.Warning,
        Message = "Callback query {CallbackQueryId} is missing message context.")]
    private static partial void LogCallbackQueryMissingMessageContext(ILogger logger, string callbackQueryId);

    [LoggerMessage(
        EventId = LogEventIds.InvalidStoryIdFormat,
        Level = LogLevel.Warning,
        Message = "Invalid story ID format in callback data: {CallbackData}")]
    private static partial void LogInvalidStoryIdFormat(ILogger logger, string? callbackData);

    [LoggerMessage(
        EventId = LogEventIds.UnbanUserFailed,
        Level = LogLevel.Error,
        Message = "Error occurred while unbanning user for story {StoryId}.")]
    private static partial void LogUnbanUserFailed(ILogger logger, Exception exception, Guid storyId);
}
