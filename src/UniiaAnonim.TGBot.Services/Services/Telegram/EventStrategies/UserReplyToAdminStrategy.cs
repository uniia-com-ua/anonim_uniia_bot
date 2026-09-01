using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling messages from users who have an active story.
/// Forwards their messages to the admin channels with the attached story ID.
/// </summary>
public sealed class UserReplyToAdminStrategy(
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    ILogger<UserReplyToAdminStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public async Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        var msg = update.Message;

        return msg is not null &&
               msg.Chat.Type == ChatType.Private &&
               msg.From is { IsBot: false } &&
               await storyAuthorService.HasUserActiveStoryAsync(msg.From.Id, ct);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var message = update.Message;
        var userId = message.From.Id;

        var storyMessageId = await storyAuthorService.GetActualStoryMessageIdAsync(userId, ct);

        var adminChannel = await channelRepository.GetByTypeAsync(ChannelType.AdminChannel, ct);
        if (adminChannel is null)
        {
            logger.LogWarning("Admin channel not found in the database. Cannot forward user reply.");
            return;
        }

        var replyParams = new ReplyParameters
        {
            MessageId = storyMessageId,
        };

        try
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                await botClient.SendMessage(
                    chatId: adminChannel.ChannelId,
                    text: localizer["str0006", message.Text],
                    parseMode: ParseMode.Html,
                    replyParameters: replyParams,
                    cancellationToken: ct);
            }
            else
            {
                var caption = !string.IsNullOrEmpty(message.Caption)
                    ? localizer["str0006", message.Caption]
                    : (string?)null;

                await botClient.CopyMessage(
                    chatId: adminChannel.ChannelId,
                    fromChatId: message.Chat.Id,
                    messageId: message.MessageId,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyParameters: replyParams,
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send user reply for story message {StoryMessageId} to admin channel {ChannelId}.", storyMessageId, adminChannel.ChannelId);
        }
    }
}