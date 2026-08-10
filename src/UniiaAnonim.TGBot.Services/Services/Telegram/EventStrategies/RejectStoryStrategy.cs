using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the rejection of a story by an administrator.
/// Removes the story from the queue and notifies the author about the rejection.
/// </summary>
public sealed class RejectStoryStrategy(
    ITelegramBotClient botClient,
    IStoryAuthorService storyAuthorService,
    IStoryAuthorRepository storyAuthorRepository,
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
        var message = callbackQuery.Message;

        if (message is null)
        {
            logger.LogWarning("Callback query {CallbackQueryId} is missing message context.", callbackQuery.Id);
            return;
        }

        var storyIdString = callbackQuery.Data[ButtonPrefixes.AdminRejectStoryPrefix.Length..];

        if (!Guid.TryParse(storyIdString, out var storyId))
        {
            logger.LogWarning("Invalid story ID format in callback data: {CallbackData}", callbackQuery.Data);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0007"], cancellationToken: ct);
            return;
        }

        try
        {
            var authorTelegramId = await storyAuthorService.GetDecryptedTelegramIdAsync(storyId, ct);

            await storyAuthorRepository.DeleteAsync(storyId, ct);
            await storyAuthorRepository.SaveChangesAsync(ct);

            await botClient.SendMessage(
                chatId: authorTelegramId,
                text: localizer["str0008"],
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            var updatedText = message.Text ?? message.Caption;
            if (!string.IsNullOrEmpty(updatedText))
            {
                var formattedText = localizer["str0009", updatedText].UnescapedValue();
                await SafeEditMessageOrCaptionAsync(message.Chat.Id, message.MessageId, formattedText, ct);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: localizer["str0010"],
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while rejecting story {StoryId}.", storyId);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, localizer["str0011"], showAlert: true, cancellationToken: ct);
        }
    }

    /// <summary>
    /// Helper method to safely update either message text or caption.
    /// </summary>
    private async Task SafeEditMessageOrCaptionAsync(long chatId, int messageId, string text, CancellationToken ct)
    {
        try
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: null,
                cancellationToken: ct);
        }
        catch
        {
            await botClient.EditMessageCaption(
                chatId: chatId,
                messageId: messageId,
                caption: text,
                parseMode: ParseMode.Html,
                replyMarkup: null,
                cancellationToken: ct);
        }
    }
}