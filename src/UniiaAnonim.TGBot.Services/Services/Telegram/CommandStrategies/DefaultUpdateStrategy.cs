using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.CommandStrategies;

public sealed partial class DefaultUpdateStrategy(
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    ILogger<DefaultUpdateStrategy> logger)
    : IDefaultTelegramUpdateStrategy
{
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update is not { Type: UpdateType.Message, Message: { Chat.Type: ChatType.Private } message })
        {
            return;
        }

        LogExecutingDefaultStrategy(logger, message.Chat.Id, update.Id);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: localizer["str0027"],
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Executing default update strategy for chat {ChatId} and update {UpdateId}.")]
    private static partial void LogExecutingDefaultStrategy(ILogger logger, long chatId, int updateId);
}