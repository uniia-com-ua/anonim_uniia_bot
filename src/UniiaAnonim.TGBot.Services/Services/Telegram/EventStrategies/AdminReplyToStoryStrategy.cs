using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
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

        return string.IsNullOrEmpty(repliedText) || repliedText.Contains(MessageMarkers.EditMarker, StringComparison.Ordinal)
            ? Task.FromResult(false)
            : Task.FromResult(true);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var message = update.Message;
        var originalText = message.ReplyToMessage?.Text ?? message.ReplyToMessage?.Caption;

        if (string.IsNullOrWhiteSpace(originalText))
        {
            return;
        }

        var match = GuidRegex().Match(originalText);
        if (!match.Success || !Guid.TryParse(match.Value, out var storyId))
        {
            logger.LogDebug("No story ID (Guid) found in the replied message text.");
            return;
        }

        if (!await channelRepository.ExistsAsync(message.Chat.Id, ct))
        {
            logger.LogWarning("Chat {ChatId} is not a registered admin channel. Ignoring reply.", message.Chat.Id);
            return;
        }

        if (!await storyAuthorService.ExistsAsync(storyId, ct))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0016"],
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);

            return;
        }

        if (await storyAuthorService.IsPublishedAsync(storyId, ct))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0017"],
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
            logger.LogError(ex, "Failed to copy admin reply to user {TelegramId} for story {StoryId}.", authorId, storyId);
        }
    }

    /// <summary>
    /// Provides a compiled regular expression for extracting a Guid from a text string.
    /// </summary>
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();
}