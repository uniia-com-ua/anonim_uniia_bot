using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
public sealed class AuthorReviewEditStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorRepository storyAuthorRepository,
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
        var callback = update.CallbackQuery!;
        var message = callback.Message;

        if (!TryExtractCallbackData(callback.Data!, out var isApproved, out var storyId))
        {
            logger.LogWarning("Invalid story ID format in callback data: {CallbackData}", callback.Data);
            return;
        }

        await RemoveInlineKeyboardAsync(message!, ct);

        var adminChat = await channelRepository.GetByTypeAsync(ChannelType.AdminChannel, ct);
        if (adminChat is null)
        {
            logger.LogWarning("Admin channel not found in the database. Cannot update message.");
            return;
        }

        var storyAuthor = await storyAuthorRepository.GetAsync(storyId, ct);
        if (storyAuthor is null || storyAuthor.ChannelMessageId == default)
        {
            logger.LogWarning("Story author or MessageId not found for story ID {StoryId}.", storyId);
            return;
        }

        var updatedText = ExtractStoryContent(message?.Text ?? message?.Caption ?? string.Empty);

        if (isApproved)
        {
            await ProcessApprovalAsync(callback.Id, storyId, storyAuthor.ChannelMessageId, adminChat.ChannelId, updatedText, ct);
        }
        else
        {
            await ProcessRejectionAsync(message.Chat.Id, storyAuthor.ChannelMessageId, adminChat.ChannelId, ct);
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

        var messageText = localizer["str0004", storyId, updatedText];
        await SafeEditMessageOrCaptionAsync(adminChatId, adminMessageId, adminKeyboard, messageText, ct);
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
    /// Helper method to safely update either message text, caption, or just reply markup depending on the message type.
    /// </summary>
    private async Task SafeEditMessageOrCaptionAsync(long chatId, int messageId, InlineKeyboardMarkup? replyMarkup, string text, CancellationToken ct = default)
    {
        try
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }
        catch
        {
            await botClient.EditMessageCaption(
                chatId: chatId,
                messageId: messageId,
                caption: text,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }
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
}