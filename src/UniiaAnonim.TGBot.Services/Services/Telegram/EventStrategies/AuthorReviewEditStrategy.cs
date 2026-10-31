using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Extensions;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the author's decision regarding an edited story.
/// Processes the approval or rejection callback and updates the original admin message.
/// </summary>
public sealed partial class AuthorReviewEditStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IChannelRepository channelRepository,
    IAdminActionKeyboardFactory keyboardFactory,
    IStringLocalizer<Messages> localizer,
    ILogger<AuthorReviewEditStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            (update.CallbackQuery.Data.StartsWith(ButtonPrefixes.UserApproveStoryPrefix, StringComparison.OrdinalIgnoreCase)
            || update.CallbackQuery.Data.StartsWith(ButtonPrefixes.UserRejectStoryPrefix, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var callback = update.CallbackQuery;
        var message = callback?.Message;

        if (callback is null || message is null)
        {
            LogCallbackQueryMissingContext(logger);
            return;
        }

        if (!TryExtractCallbackData(callback.Data!, out var isApproved, out var storyId))
        {
            LogInvalidStoryIdFormat(logger, callback.Data);
            return;
        }

        await RemoveInlineKeyboardAsync(message, ct);

        var adminChat = await channelRepository.GetByTypeAsync(ChannelType.AdminChannel, ct);
        if (adminChat is null)
        {
            LogAdminChannelNotFoundForUpdateMessage(logger);
            return;
        }

        var messageIds = await storyAuthorService.GetAsync(storyId, ct);
        if (messageIds.InteractiveMessageId == default)
        {
            LogStoryAuthorOrMessageNotFound(logger, storyId);
            return;
        }

        var updatedText = ExtractStoryContent(message.Text ?? message.Caption ?? string.Empty);

        if (isApproved)
        {
            await ProcessApprovalAsync(callback.Id, storyId, messageIds.InteractiveMessageId, adminChat.ChannelId, updatedText, ct);
        }
        else
        {
            await ProcessRejectionAsync(message.Chat.Id, messageIds.InteractiveMessageId, adminChat.ChannelId, ct);
        }
    }

    private static bool TryExtractCallbackData(string callbackData, out bool isApproved, out Guid storyId)
    {
        isApproved = callbackData.StartsWith(ButtonPrefixes.UserApproveStoryPrefix, StringComparison.OrdinalIgnoreCase);
        var prefix = isApproved ? ButtonPrefixes.UserApproveStoryPrefix : ButtonPrefixes.UserRejectStoryPrefix;

        var storyIdString = callbackData[prefix.Length..];
        return Guid.TryParse(storyIdString, out storyId);
    }

    private async Task RemoveInlineKeyboardAsync(Message message, CancellationToken ct)
    {
        await botClient.EditMessageReplyMarkup(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            replyMarkup: null,
            cancellationToken: ct);
    }

    private async Task ProcessApprovalAsync(string callbackId, Guid storyId, int adminMessageId, long adminChatId, string updatedText, CancellationToken ct)
    {
        await botClient.AnswerCallbackQuery(callbackId, localizer["str0025"], cancellationToken: ct);

        var adminKeyboard = keyboardFactory.CreateModerationKeyboard(storyId);

        await botClient.SendMessage(
            chatId: adminChatId,
            text: localizer["str0023"].UnescapedValue(),
            replyParameters: new ReplyParameters { MessageId = adminMessageId },
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        var messageText = localizer["str0004", storyId, updatedText.EscapeTelegramHtml()].UnescapedValue();
        await botClient.SafeEditMessageOrCaptionAsync(adminChatId, adminMessageId, messageText, adminKeyboard, ct: ct);
    }

    private async Task ProcessRejectionAsync(long userChatId, int adminMessageId, long adminChatId, CancellationToken ct)
    {
        await botClient.SendMessage(
            chatId: userChatId,
            text: localizer["str0026"].UnescapedValue(),
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await botClient.SendMessage(
            chatId: adminChatId,
            text: localizer["str0024"].UnescapedValue(),
            replyParameters: new ReplyParameters { MessageId = adminMessageId },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    /// <summary>
    /// Extracts the actual story text from the admin message format by locating the marker.
    /// </summary>
    private static string ExtractStoryContent(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
        {
            return string.Empty;
        }

        var index = fullText.IndexOf(MessageMarkers.EditedHistoryTextMarker, StringComparison.Ordinal);

        return index >= 0 ? fullText[(index + MessageMarkers.EditedHistoryTextMarker.Length)..].Trim() : fullText;
    }

    [LoggerMessage(
        EventId = LogEventIds.CallbackQueryMissingContext,
        Level = LogLevel.Warning,
        Message = "Callback query is missing context.")]
    private static partial void LogCallbackQueryMissingContext(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.InvalidStoryIdFormat,
        Level = LogLevel.Warning,
        Message = "Invalid story ID format in callback data: {CallbackData}")]
    private static partial void LogInvalidStoryIdFormat(ILogger logger, string? callbackData);

    [LoggerMessage(
        EventId = LogEventIds.AdminChannelNotFoundForUpdateMessage,
        Level = LogLevel.Warning,
        Message = "Admin channel not found in the database. Cannot update message.")]
    private static partial void LogAdminChannelNotFoundForUpdateMessage(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.StoryAuthorOrMessageNotFound,
        Level = LogLevel.Warning,
        Message = "Story author or MessageId not found for story ID {StoryId}.")]
    private static partial void LogStoryAuthorOrMessageNotFound(ILogger logger, Guid storyId);
}