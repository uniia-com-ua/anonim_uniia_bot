using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Exceptions;
using UniiaAnonim.TGBot.Shared.Extensions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Orchestrates the dispatching of incoming anonymous stories to administrative channels,
/// ensuring author registration and proper payload formatting.
/// </summary>
public sealed partial class StoryDispatcherService(
    ITelegramBotClient botClient,
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
    IHashService hashService,
    IStringLocalizer<Messages> localizer,
    IAdminActionKeyboardFactory keyboardFactory,
    ITelegramDeliveryService deliveryService,
    ILogger<StoryDispatcherService> logger)
    : IStoryDispatcherService
{
    /// <inheritdoc/>
    public async Task ProcessAsync(StoryAuthorDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userHash = hashService.ComputeHash(dto.TelegramId.ToString(CultureInfo.InvariantCulture));
        var adminChannel = await channelRepository.GetByTypeAsync(ChannelType.AdminChannel, ct);

        if (adminChannel is null)
        {
            LogNoAdminChannelsFound(logger, userHash);
            return;
        }

        var storyId = await storyAuthorService.CreateAsync(dto.TelegramId, ct);
        var inlineKeyboard = keyboardFactory.CreateModerationKeyboard(storyId);

        try
        {
            var messageIds = await deliveryService.DeliverToAdminAsync(
                chatId: adminChannel.ChannelId,
                text: localizer["str0004", storyId, dto.Story.EscapeTelegramHtml()].UnescapedValue(),
                files: dto.Files,
                keyboard: inlineKeyboard,
                ct: ct);

            await storyAuthorService.SetMessageIdAsync(storyId, messageIds, ct);
        }
        catch (StoryTooLongException ex)
        {
            LogStoryLengthExceededLimit(logger, userHash, ex.CurrentLength, ex.MaxLength);

            await storyAuthorService.DeleteAsync(storyId, ct);

            await botClient.SendMessage(
                chatId: dto.TelegramId,
                text: localizer["str0036"].UnescapedValue(),
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }
        catch (Exception ex)
        {
            LogDispatchStoryFailed(logger, ex, adminChannel.ChannelId);

            await storyAuthorService.DeleteAsync(storyId, ct);

            throw new StoryDispatchFailedException(storyId, userHash, ex);
        }

        await botClient.SendMessage(
            chatId: dto.TelegramId,
            text: localizer["str0005"].UnescapedValue(),
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    [LoggerMessage(
        EventId = LogEventIds.NoAdminChannelsFound,
        Level = LogLevel.Warning,
        Message = "No admin channels found. Story from user {UserHash} was not dispatched.")]
    private static partial void LogNoAdminChannelsFound(ILogger logger, string userHash);

    [LoggerMessage(
        EventId = LogEventIds.StoryLengthExceededLimit,
        Level = LogLevel.Warning,
        Message = "Story submission rejected for user {UserHash}: character length {Length} exceeds {Limit}")]
    private static partial void LogStoryLengthExceededLimit(ILogger logger, string userHash, int length, int limit);

    [LoggerMessage(
        EventId = LogEventIds.DispatchStoryFailed,
        Level = LogLevel.Error,
        Message = "Failed to dispatch story to admin channel {ChannelId}.")]
    private static partial void LogDispatchStoryFailed(ILogger logger, Exception exception, long channelId);
}