using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
/// Defines a strategy for handling administrator replies containing edited story text.
/// Extracts the edited text and sends it to the original author for approval.
/// </summary>
public sealed partial class AdminSubmitEditedStoryStrategy(
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
    ITelegramBotClient botClient,
    IHashService hashService,
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

        var replyText = msg.ReplyToMessage.Text ?? msg.ReplyToMessage.Caption;
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(replyText) &&
            replyText.Contains(MessageMarkers.EditedHistoryTextMarker, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var message = update.Message;
        var replyText = message?.ReplyToMessage?.Text ?? message?.ReplyToMessage?.Caption;

        if (message is null || string.IsNullOrWhiteSpace(replyText))
        {
            return;
        }

        var matchEnumerator = CommonRegexes.GuidRegex().EnumerateMatches(replyText);
        if (!matchEnumerator.MoveNext() || !Guid.TryParse(replyText.AsSpan(matchEnumerator.Current.Index, matchEnumerator.Current.Length), out var storyId))
        {
            LogStoryIdNotFoundInEditPrompt(logger);
            return;
        }

        if (!await channelRepository.ExistsAsync(message.Chat.Id, ct))
        {
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
        var editedText = message.Text ?? message.Caption ?? string.Empty;

        var keyboard = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(localizer["str0020"], ButtonPrefixes.GetUserApproveStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(localizer["str0021"], ButtonPrefixes.GetUserRejectStoryButtonPrefix(storyId))]
        ]);

        try
        {
            await botClient.SendMessage(
                chatId: authorId,
                text: localizer["str0019", editedText.EscapeTelegramHtml()].UnescapedValue(),
                replyMarkup: keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: localizer["str0022"].UnescapedValue(),
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            var authorHash = hashService.ComputeHash(authorId.ToString(CultureInfo.InvariantCulture));
            LogSendEditedStoryFailed(logger, ex, authorHash, storyId);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.StoryIdNotFoundInEditPrompt,
        Level = LogLevel.Debug,
        Message = "No story ID (Guid) found in the edit prompt.")]
    private static partial void LogStoryIdNotFoundInEditPrompt(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.SendEditedStoryFailed,
        Level = LogLevel.Error,
        Message = "Failed to send edited story to user {UserHash} for story {StoryId}.")]
    private static partial void LogSendEditedStoryFailed(ILogger logger, Exception exception, string userHash, Guid storyId);
}