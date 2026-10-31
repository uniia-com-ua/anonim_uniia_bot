using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Extensions;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for banning a user and updating their submitted story moderation message.
/// Records the ban in the database, displays an unban button to allow reversing the ban,
/// and sends no notification to the author.
/// </summary>
public sealed partial class BanUserStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IBannedUserService bannedUserService,
    IStringLocalizer<Messages> localizer,
    ILogger<BanUserStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.StartsWith(ButtonPrefixes.AdminBanUserPrefix, StringComparison.OrdinalIgnoreCase));
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

        var storyIdString = callbackQuery.Data[ButtonPrefixes.AdminBanUserPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            LogInvalidStoryIdFormat(logger, callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);

            await bannedUserService.BanUserAsync(authorTelegramId, "Banned by moderator", ct);

            var unbanKeyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData(
                    localizer["str0041"],
                    ButtonPrefixes.GetAdminUnbanUserButtonPrefix(storyId)));

            var updatedText = message.Text ?? message.Caption;
            if (!string.IsNullOrEmpty(updatedText))
            {
                var formattedText = localizer["str0039", updatedText.EscapeTelegramHtml()].UnescapedValue();
                await botClient.SafeEditMessageOrCaptionAsync(
                    message.Chat.Id,
                    message.MessageId,
                    formattedText,
                    replyMarkup: unbanKeyboard,
                    ct: ct);
            }
            else
            {
                await botClient.EditMessageReplyMarkup(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    replyMarkup: unbanKeyboard,
                    cancellationToken: ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0038"],
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            LogBanUserFailed(logger, ex, storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0040"], showAlert: true, cancellationToken: ct);
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
        EventId = LogEventIds.BanUserFailed,
        Level = LogLevel.Error,
        Message = "Error occurred while banning user for story {StoryId}.")]
    private static partial void LogBanUserFailed(ILogger logger, Exception exception, Guid storyId);
}
