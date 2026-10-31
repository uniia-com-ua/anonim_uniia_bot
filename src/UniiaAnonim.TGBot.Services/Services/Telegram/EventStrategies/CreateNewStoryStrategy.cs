using System.Globalization;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling messages from users who DO NOT have an active story.
/// Collects the message data and delegates it to the StoryDispatcherService to start a new story.
/// </summary>
public sealed partial class CreateNewStoryStrategy(
    IStoryAuthorService storyAuthorService,
    IStoryDispatcherService storyDispatcherService,
    ITelegramMediaProcessor telegramMediaProcessor,
    IHashService hashService,
    ILogger<CreateNewStoryStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public async Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        var msg = update.Message;

        return msg is not null &&
               msg.Chat.Type == ChatType.Private &&
               msg.From is { IsBot: false } &&
               !await storyAuthorService.HasUserActiveStoryAsync(msg.From.Id, ct);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update.Message is not { From: not null } message)
        {
            return;
        }

        var userId = message.From.Id;
        var text = message.Text ?? message.Caption ?? string.Empty;
        var mediaFiles = telegramMediaProcessor.ExtractMediaFiles(message);

        var dto = new StoryAuthorDto(
            TelegramId: userId,
            Story: text,
            Files: mediaFiles);

        try
        {
            await storyDispatcherService.ProcessAsync(dto, ct);
        }
        catch (Exception ex)
        {
            var userHash = hashService.ComputeHash(userId.ToString(CultureInfo.InvariantCulture));
            LogCreateNewStoryFailed(logger, ex, userHash);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.CreateNewStoryFailed,
        Level = LogLevel.Error,
        Message = "Failed to process a new story from user {UserHash}.")]
    private static partial void LogCreateNewStoryFailed(ILogger logger, Exception exception, string userHash);
}