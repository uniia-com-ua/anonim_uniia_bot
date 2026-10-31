using Microsoft.Extensions.Caching.Memory;
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
/// Defines a strategy for handling the immediate publication of a story by an administrator.
/// Forwards the clean story content to the main public channel and updates the queue.
/// </summary>
public sealed partial class PublishStoryStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    ITelegramMediaProcessor telegramMediaProcessor,
    IChannelRepository channelRepository,
    IStringLocalizer<Messages> localizer,
    IMemoryCache memoryCache,
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
        var message = callbackQuery?.Message;

        if (callbackQuery is null || message is null)
        {
            LogCallbackQueryMessageContextMissing(logger);
            return;
        }

        var storyIdString = callbackQuery.Data?[ButtonPrefixes.AdminPublishStoryPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            LogInvalidStoryIdFormat(logger, callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var targetChannel = await channelRepository.GetByTypeAsync(ChannelType.PublicChannel, ct);
            if (targetChannel is null)
            {
                LogPublicChannelNotConfigured(logger, storyId);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0015"], showAlert: true, cancellationToken: ct);
                return;
            }

            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);
            var storyEntity = await storyAuthorService.GetAsync(storyId, ct);

            var originalContent = message.Text ?? message.Caption ?? string.Empty;
            var cleanStoryContent = await ModifyStoryContentAsync(originalContent, ct);

            if (storyEntity?.MediaFiles is { Count: > 0 })
            {
                await DeliverStoryMediaAsync(targetChannel.ChannelId, storyEntity.MediaFiles, cleanStoryContent, ct);
            }
            else if (message.Type != MessageType.Text)
            {
                await botClient.CopyMessage(
                    chatId: targetChannel.ChannelId,
                    fromChatId: message.Chat.Id,
                    messageId: message.MessageId,
                    caption: cleanStoryContent,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: targetChannel.ChannelId,
                    text: cleanStoryContent,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }

            await botClient.SendMessage(
                chatId: authorTelegramId,
                text: localizer["str0014"].UnescapedValue(),
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            var originalAdminText = message.Text ?? message.Caption;
            if (!string.IsNullOrEmpty(originalAdminText))
            {
                var formattedText = localizer["str0012", originalAdminText.EscapeTelegramHtml()].UnescapedValue();
                await botClient.SafeEditMessageOrCaptionAsync(message.Chat.Id, message.MessageId, formattedText, ct: ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0013"],
                cancellationToken: ct);

            await storyAuthorService.SetStatusAsync(storyId, StoryStatus.Published, ct);
        }
        catch (Exception ex)
        {
            LogPublishStoryFailed(logger, ex, storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0015"], showAlert: true, cancellationToken: ct);
        }
    }

    private async Task DeliverStoryMediaAsync(
        long targetChatId,
        Dictionary<string, StoryMediaType> mediaFiles,
        string cleanContent,
        CancellationToken ct)
    {
        var isLongText = cleanContent.Length > MaxCaptionLength;

        if (mediaFiles.Count == 1)
        {
            var single = mediaFiles.First();
            var inputFile = InputFile.FromFileId(single.Key);

            if (isLongText)
            {
                var mediaMessage = await SendSingleMediaAsync(targetChatId, single.Value, inputFile, caption: null, ct);
                await botClient.SendMessage(
                    chatId: targetChatId,
                    text: cleanContent,
                    parseMode: ParseMode.Html,
                    replyParameters: new ReplyParameters { MessageId = mediaMessage.MessageId },
                    cancellationToken: ct);
            }
            else
            {
                await SendSingleMediaAsync(targetChatId, single.Value, inputFile, caption: cleanContent, ct);
            }

            return;
        }

        var mediaGroup = telegramMediaProcessor.ConvertToAlbumMedia(mediaFiles, isLongText ? null : cleanContent);

        if (isLongText)
        {
            var mediaMessages = await botClient.SendMediaGroup(
                chatId: targetChatId,
                media: mediaGroup,
                cancellationToken: ct);

            await botClient.SendMessage(
                chatId: targetChatId,
                text: cleanContent,
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = mediaMessages[0].MessageId },
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

    private Task<Message> SendSingleMediaAsync(
        long chatId,
        StoryMediaType mediaType,
        InputFile file,
        string? caption,
        CancellationToken ct)
    {
        var parseMode = caption is not null ? ParseMode.Html : ParseMode.None;

        return mediaType switch
        {
            StoryMediaType.Photo => botClient.SendPhoto(chatId, file, caption: caption, parseMode: parseMode, cancellationToken: ct),
            StoryMediaType.Video => botClient.SendVideo(chatId, file, caption: caption, parseMode: parseMode, cancellationToken: ct),
            StoryMediaType.Document => botClient.SendDocument(chatId, file, caption: caption, parseMode: parseMode, cancellationToken: ct),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, "Unsupported media type."),
        };
    }

    /// <summary>
    /// Extracts and modifies the story content from the full text, removing any markers and adding bot information.
    /// </summary>
    private async Task<string> ModifyStoryContentAsync(string fullText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullText))
        {
            return string.Empty;
        }

        var index = fullText.IndexOf(MessageMarkers.StoryMarker, StringComparison.Ordinal);
        var story = index >= 0 ? fullText[(index + MessageMarkers.StoryMarker.Length)..].Trim() : fullText;

        const string cacheKey = "tg_bot_username";
        if (!memoryCache.TryGetValue(cacheKey, out string? botUsername) || botUsername is null)
        {
            var me = await botClient.GetMe(ct);
            botUsername = me.Username ?? string.Empty;
            memoryCache.Set(cacheKey, botUsername, TimeSpan.FromHours(24));
        }

        return localizer["str0035", story.EscapeTelegramHtml(), botUsername].UnescapedValue();
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
        EventId = LogEventIds.PublicChannelNotConfigured,
        Level = LogLevel.Warning,
        Message = "Public channel is not configured in the database. Cannot publish story {StoryId}.")]
    private static partial void LogPublicChannelNotConfigured(ILogger logger, Guid storyId);

    [LoggerMessage(
        EventId = LogEventIds.PublishStoryFailed,
        Level = LogLevel.Error,
        Message = "Error occurred while publishing story {StoryId}.")]
    private static partial void LogPublishStoryFailed(ILogger logger, Exception exception, Guid storyId);
}