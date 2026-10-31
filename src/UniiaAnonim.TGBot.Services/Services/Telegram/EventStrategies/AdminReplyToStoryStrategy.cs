using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling replies to bot messages within registered admin groups.
/// Extracts the story identifier, decrypts the author's Telegram ID, and forwards the admin's reply to the author.
/// </summary>
public sealed partial class AdminReplyToStoryStrategy(
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
    ITelegramBotClient botClient,
    IHashService hashService,
    IStringLocalizer<Messages> localizer,
    ILogger<AdminReplyToStoryStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        var msg = update.Message;
        if (msg is null ||
            msg.Chat.Type is not (ChatType.Group or ChatType.Supergroup) ||
            msg.ReplyToMessage is null ||
            msg.ReplyToMessage.From?.IsBot != true)
        {
            return Task.FromResult(false);
        }

        var repliedText = msg.ReplyToMessage.Text ?? msg.ReplyToMessage.Caption;

        return Task.FromResult(
            !string.IsNullOrEmpty(repliedText) &&
            !repliedText.Contains(MessageMarkers.EditMarker, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var message = update.Message;
        var originalText = message?.ReplyToMessage?.Text ?? message?.ReplyToMessage?.Caption;

        if (message is null || string.IsNullOrWhiteSpace(originalText))
        {
            return;
        }

        var matchEnumerator = CommonRegexes.GuidRegex().EnumerateMatches(originalText);
        if (!matchEnumerator.MoveNext() || !Guid.TryParse(originalText.AsSpan(matchEnumerator.Current.Index, matchEnumerator.Current.Length), out var storyId))
        {
            LogStoryIdNotFoundInReplyMessage(logger);
            return;
        }

        if (!await channelRepository.ExistsAsync(message.Chat.Id, ct))
        {
            LogAdminChannelNotFound(logger, message.Chat.Id);
            return;
        }

        if (await storyAuthorService.IsInStatusAsync(storyId, StoryStatus.Published, ct))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0017"].UnescapedValue(),
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);

            return;
        }

        var authorId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);

        try
        {
            await botClient.CopyMessage(
                chatId: authorId,
                fromChatId: message.Chat.Id,
                messageId: message.MessageId,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            var authorHash = hashService.ComputeHash(authorId.ToString(CultureInfo.InvariantCulture));
            LogCopyAdminReplyFailed(logger, ex, authorHash, storyId);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.StoryIdNotFoundInReplyMessage,
        Level = LogLevel.Debug,
        Message = "No story ID (Guid) found in the replied message text.")]
    private static partial void LogStoryIdNotFoundInReplyMessage(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.AdminChannelNotFound,
        Level = LogLevel.Warning,
        Message = "Chat {ChatId} is not a registered admin channel. Ignoring reply.")]
    private static partial void LogAdminChannelNotFound(ILogger logger, long chatId);

    [LoggerMessage(
        EventId = LogEventIds.CopyAdminReplyFailed,
        Level = LogLevel.Error,
        Message = "Failed to copy admin reply to user {UserHash} for story {StoryId}.")]
    private static partial void LogCopyAdminReplyFailed(ILogger logger, Exception exception, string userHash, Guid storyId);
}