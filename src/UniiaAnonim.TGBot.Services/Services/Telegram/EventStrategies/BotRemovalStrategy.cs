using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the my_chat_member update.
/// Removes the chat (group or channel) from the database when the bot is removed or leaves.
/// </summary>
public sealed class BotRemovalStrategy(
    IChannelRepository channelRepository,
    ILogger<BotRemovalStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.MyChatMember, MyChatMember: not null } &&
            update.MyChatMember.NewChatMember.Status is ChatMemberStatus.Left or ChatMemberStatus.Kicked);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var chat = update.MyChatMember.Chat;

        if (!await channelRepository.ExistsAsync(chat.Id, ct))
        {
            logger.LogWarning("Channel with ID {ChannelId} does not exist in the database. Skipping removal.", chat.Id);
            return;
        }

        await channelRepository.DeleteByChannelIdAsync(chat.Id, ct);
        await channelRepository.SaveChangesAsync(ct);
    }
}