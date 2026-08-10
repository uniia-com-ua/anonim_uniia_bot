using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

/// <summary>
/// Defines a strategy for handling the my_chat_member update.
/// Registers the chat (group or channel) in the database when the bot is added or promoted.
/// </summary>
public sealed class BotRegistrationStrategy(
    IChannelRepository channelRepository,
    ILogger<BotRegistrationStrategy> logger)
    : ITelegramUpdateStrategy
{
    /// <inheritdoc/>
    public Task<bool> CanHandleAsync(Update update, CancellationToken ct = default)
    {
        return Task.FromResult(
            update is { Type: UpdateType.MyChatMember, MyChatMember: not null } &&
            update.MyChatMember.NewChatMember.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        var chat = update.MyChatMember.Chat;

        var channelType = chat.Type switch
        {
            ChatType.Channel => ChannelType.PublicChannel,
            ChatType.Group or ChatType.Supergroup => ChannelType.AdminChannel,
            ChatType.Private or ChatType.Sender or _ => (ChannelType?)null,
        };

        if (channelType is null)
        {
            logger.LogWarning("Unsupported chat type {ChatType} encountered for chat ID {ChatId}.", chat.Type, chat.Id);
            return;
        }

        if (await channelRepository.ExistsAsync(chat.Id, ct))
        {
            logger.LogWarning("Channel with ID {ChannelId} already exists in the database. Skipping registration.", chat.Id);
            return;
        }

        var channel = new Channel
        {
            ChannelId = chat.Id,
            Type = channelType.Value,
        };

        await channelRepository.CreateAsync(channel, ct);
        await channelRepository.SaveChangesAsync(ct);
    }
}