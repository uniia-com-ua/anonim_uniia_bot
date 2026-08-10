using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the immediate publication of a story by an administrator.
/// Forwards the clean story content to the main public channel and updates the queue.
/// </summary>
public sealed class PublishStoryStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    ITelegramMediaProcessor telegramMediaProcessor,
    IChannelRepository channelRepository,
    IStringLocalizer<Messages> localizer,
    ILogger<PublishStoryStrategy> logger)
    : ITelegramUpdateStrategy
{
    private const int MaxCaptionLength = 1024;

    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null } &&
            update.CallbackQuery.Data.StartsWith(ButtonPrefixes.AdminPublishStoryPrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var callbackQuery = update.CallbackQuery;
        var message = callbackQuery.Message;

        if (message is null)
        {
            logger.LogWarning("Callback query {CallbackQueryId} is missing message context.", callbackQuery.Id);
            return;
        }

        var storyIdString = callbackQuery.Data[ButtonPrefixes.AdminPublishStoryPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            logger.LogWarning("Invalid story ID format in callback data: {CallbackData}", callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var targetChannel = await channelRepository.GetByTypeAsync(ChannelType.PublicChannel, ct);
            if (targetChannel is null)
            {
                logger.LogWarning("Public channel is not configured in the database. Cannot publish story {StoryId}.", storyId);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0015"], showAlert: true, cancellationToken: ct);
                return;
            }

            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);
            var storyEntity = await storyAuthorService.GetAsync(storyId, ct);

            await storyAuthorService.MarkAsPublishedAsync(storyId, ct);

            var originalContent = message.Text ?? message.Caption ?? string.Empty;
            var cleanStoryContent = ExtractStoryContent(originalContent);

            if (storyEntity?.MediaFiles is { Count: > 0 })
            {
                await SendMediaGroupStoryAsync(targetChannel.ChannelId, storyEntity.MediaFiles, cleanStoryContent, ct);
            }
            else if (message.Type != MessageType.Text)
            {
                await botClient.EditMessageCaption(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    caption: cleanStoryContent,
                    parseMode: ParseMode.Html,
                    replyMarkup: null,
                    cancellationToken: ct);

                await botClient.CopyMessage(
                    chatId: targetChannel.ChannelId,
                    fromChatId: message.Chat.Id,
                    messageId: message.MessageId,
                    cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: targetChannel.ChannelId,
                    text: cleanStoryContent,
                    cancellationToken: ct);
            }

            await botClient.SendMessage(
                chatId: authorTelegramId,
                text: localizer["str0014"],
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            if (!string.IsNullOrEmpty(message.Text))
            {
                await botClient.EditMessageText(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: localizer["str0012", message.Text].UnescapedValue(),
                    parseMode: ParseMode.Html,
                    replyMarkup: null,
                    cancellationToken: ct);
            }
            else if (!string.IsNullOrEmpty(message.Caption))
            {
                await botClient.EditMessageCaption(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    caption: localizer["str0012", message.Caption].UnescapedValue(),
                    parseMode: ParseMode.Html,
                    replyMarkup: null,
                    cancellationToken: ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0013"],
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while publishing story {StoryId}.", storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0015"], showAlert: true, cancellationToken: ct);
        }
    }

    private async Task SendMediaGroupStoryAsync(long targetChatId, Dictionary<string, StoryMediaType> mediaFiles, string cleanContent, CancellationToken ct)
    {
        var isLongText = cleanContent.Length > MaxCaptionLength;

        var mediaGroup = telegramMediaProcessor.ConvertToAlbumMedia(mediaFiles, isLongText ? null : cleanContent);

        if (isLongText)
        {
            var mediaMessage = await botClient.SendMediaGroup(
                chatId: targetChatId,
                media: mediaGroup,
                cancellationToken: ct);

            await botClient.SendMessage(
                chatId: targetChatId,
                text: cleanContent,
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = mediaMessage[0].Id },
                cancellationToken: ct);
        }
        else
        {
            await botClient.SendMediaGroup(
                chatId: targetChatId,
                media: mediaGroup,
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

        var index = fullText.IndexOf(MessageMarkers.StoryMarker, StringComparison.Ordinal);

        return index >= 0 ? fullText[(index + MessageMarkers.StoryMarker.Length)..].Trim() : fullText;
    }
}