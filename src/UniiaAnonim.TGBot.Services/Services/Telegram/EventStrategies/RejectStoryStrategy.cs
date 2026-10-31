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
/// Defines a strategy for handling the rejection of a story by an administrator.
/// Removes the story from the queue and notifies the author about the rejection.
/// </summary>
public sealed partial class RejectStoryStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IStringLocalizer<Messages> localizer,
    ILogger<RejectStoryStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.StartsWith(ButtonPrefixes.AdminRejectStoryPrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var callbackQuery = update.CallbackQuery;
        var message = callbackQuery?.Message;

        if (callbackQuery is null || message is null)
        {
            LogCallbackQueryMessageContextMissing(logger);
            return;
        }

        var storyIdString = callbackQuery.Data?[ButtonPrefixes.AdminRejectStoryPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            LogInvalidStoryIdFormat(logger, callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);

            await storyAuthorService.DeleteAsync(storyId, ct);

            await botClient.SendMessage(
                chatId: authorTelegramId,
                text: localizer["str0008"].UnescapedValue(),
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            var updatedText = message.Text ?? message.Caption;
            if (!string.IsNullOrEmpty(updatedText))
            {
                var formattedText = localizer["str0009", updatedText.EscapeTelegramHtml()].UnescapedValue();
                await botClient.SafeEditMessageOrCaptionAsync(message.Chat.Id, message.MessageId, formattedText, ct: ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0010"],
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            LogRejectStoryFailed(logger, ex, storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0011"], showAlert: true, cancellationToken: ct);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.CallbackQueryMessageContextMissing,
        Level = LogLevel.Warning,
        Message = "Callback query is missing message context.")]
    private static partial void LogCallbackQueryMessageContextMissing(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.InvalidStoryIdFormat,
        Level = LogLevel.Warning,
        Message = "Invalid story ID format in callback data: {CallbackData}")]
    private static partial void LogInvalidStoryIdFormat(ILogger logger, string? callbackData);

    [LoggerMessage(
        EventId = LogEventIds.RejectStoryFailed,
        Level = LogLevel.Error,
        Message = "Error occurred while rejecting story {StoryId}.")]
    private static partial void LogRejectStoryFailed(ILogger logger, Exception exception, Guid storyId);
}