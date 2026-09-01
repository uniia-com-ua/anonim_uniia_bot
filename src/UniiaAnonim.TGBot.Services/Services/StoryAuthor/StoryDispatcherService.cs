using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Exceptions;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Orchestrates the dispatching of incoming anonymous stories to administrative channels,
/// ensuring author registration and proper payload formatting.
/// </summary>
public sealed class StoryDispatcherService(
    ITelegramBotClient botClient,
    IChannelRepository channelRepository,
    IStoryAuthorService storyAuthorService,
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

        var adminChannel = await channelRepository.GetByTypeAsync(ChannelType.AdminChannel, ct);

        if (adminChannel == null)
        {
            logger.LogWarning("No admin channels found. Story from user {TelegramId} was not dispatched.", dto.TelegramId);
            return;
        }

        var storyId = await storyAuthorService.CreateAsync(dto.TelegramId, ct);
        var inlineKeyboard = keyboardFactory.CreateModerationKeyboard(storyId);

        try
        {
            var messageIds = await deliveryService.DeliverToAdminAsync(
                chatId: adminChannel.ChannelId,
                text: localizer["str0004", storyId, dto.Story],
                files: dto.Files,
                keyboard: inlineKeyboard,
                ct: ct);

            await storyAuthorService.SetMessageIdAsync(storyId, messageIds, ct);
        }
        catch (StoryTooLongException)
        {
            await storyAuthorService.DeleteAsync(storyId, ct);

            await botClient.SendMessage(
                chatId: dto.TelegramId,
                text: localizer["str0005"],
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch story to admin channel {ChannelId}.", adminChannel.ChannelId);

            await storyAuthorService.DeleteAsync(storyId, ct);

            throw new StoryDispatchFailedException(storyId, dto.TelegramId, ex);
        }

        await botClient.SendMessage(
            chatId: dto.TelegramId,
            text: localizer["str0005"],
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}