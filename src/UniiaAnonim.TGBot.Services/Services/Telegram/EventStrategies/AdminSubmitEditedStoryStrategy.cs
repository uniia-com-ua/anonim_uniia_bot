using System.Text.RegularExpressions;
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
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling administrator replies containing edited story text.
/// Extracts the edited text and sends it to the original author for approval.
/// </summary>
public sealed partial class AdminSubmitEditedStoryStrategy(
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    ILogger<AdminSubmitEditedStoryStrategy> logger)
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
            repliedText.Contains(MessageMarkers.EditMarker, StringComparison.Ordinal));
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
            logger.LogDebug("No story ID (Guid) found in the edit prompt.");
            return;
        }

        if (!await channelRepository.ExistsAsync(message.Chat.Id, ct))
        {
            return;
        }

        if (!await storyAuthorService.ExistsAsync(storyId, ct))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0016"],
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);

            return;
        }

        if (await storyAuthorService.IsPublishedAsync(storyId, ct))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0017"],
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);

            return;
        }

        var authorId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);
        var editedText = message.Text ?? message.Caption ?? string.Empty;

        var keyboard = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(localizer["str0020"], ButtonPrefixes.GetUserApproveStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(localizer["str0021"], ButtonPrefixes.GetUserRejectStoryButtonPrefix(storyId))]
        ]);

        try
        {
            await botClient.SendMessage(
                chatId: authorId,
                text: localizer["str0019", editedText].UnescapedValue(),
                replyMarkup: keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0022"],
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send edited story to user {TelegramId} for story {StoryId}.", authorId, storyId);
        }
    }

    /// <summary>
    /// Provides a compiled regular expression for extracting a Guid from a text string.
    /// </summary>
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();
}