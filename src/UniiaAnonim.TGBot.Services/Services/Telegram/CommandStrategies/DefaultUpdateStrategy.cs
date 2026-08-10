using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.Telegram.CommandStrategies;

public sealed partial class DefaultUpdateStrategy(
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    IOptions<TelegramBotOptions> options,
    ILogger<DefaultUpdateStrategy> logger)
    : IDefaultTelegramUpdateStrategy
{
    private readonly TelegramBotOptions _options = options.Value;

    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        if (update is not { Type: UpdateType.Message, Message: { Chat.Type: ChatType.Private } message })
        {
            return;
        }

        InlineKeyboardMarkup? replyMarkup = null;

        if (!string.IsNullOrWhiteSpace(_options.AnonimWebformUrl))
        {
            replyMarkup = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithWebApp(
                    text: localizer["str0001"],
                    webApp: new WebAppInfo { Url = _options.AnonimWebformUrl }));
        }

        LogExecutingDefaultStrategy(logger, message.Chat.Id, update.Id);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: localizer["str0027"],
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: ct);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Executing default update strategy for chat {ChatId} and update {UpdateId}.")]
    private static partial void LogExecutingDefaultStrategy(ILogger logger, long chatId, int updateId);
}