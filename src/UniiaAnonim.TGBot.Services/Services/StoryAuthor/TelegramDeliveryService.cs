using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Implements the delivery service, handling Telegram API limits, media groups, and stream lifecycle management.
/// </summary>
public sealed class TelegramDeliveryService(
    ITelegramBotClient botClient,
    IEnumerable<IMediaTypeStrategy> mediaStrategies,
    IDefaultMediaTypeStrategy defaultMediaTypeStrategy,
    ITelegramMediaProcessor telegramMediaExtractor)
    : ITelegramDeliveryService
{
    private const int MaxCaptionLength = 1024;
    private const int MaxMessageLength = 4096;

    /// <inheritdoc/>
    public async Task<TelegramMessageIds> DeliverToAdminAsync(
        long chatId,
        string text,
        IReadOnlyDictionary<string, StoryMediaType>? files,
        InlineKeyboardMarkup keyboard,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(keyboard);

        bool hasFiles = files is not null && files.Count > 0;
        bool isLongText = text.Length > MaxCaptionLength;

        if (text.Length > MaxMessageLength)
        {
            throw new StoryTooLongException(text.Length, MaxMessageLength);
        }

        if (!hasFiles)
        {
            var message = await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);

            return new TelegramMessageIds(message.MessageId);
        }

        return files.Count == 1
                       ? await DeliverSingleFileAsync(chatId, text, files.First(), keyboard, isLongText, ct)
                       : await DeliverMediaGroupAsync(chatId, text, files, keyboard, ct);
    }

    /// <summary>
    /// Delivers a single media file to the chat.
    /// </summary>
    private async Task<TelegramMessageIds> DeliverSingleFileAsync(
        long chatId,
        string text,
        KeyValuePair<string, StoryMediaType> file,
        InlineKeyboardMarkup keyboard,
        bool isLongText,
        CancellationToken ct)
    {
        var messageType = telegramMediaExtractor.GetMessageType(file);
        var inputFile = telegramMediaExtractor.GetInputFile(file);

        var strategy = GetStrategy(messageType);

        if (isLongText)
        {
            var mediaMessage = await strategy.SendSingleAsync(chatId, inputFile, caption: null, keyboard: null, ct);

            var followUpMessage = await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                replyParameters: new ReplyParameters { MessageId = mediaMessage.MessageId },
                cancellationToken: ct);

            return new TelegramMessageIds(followUpMessage.MessageId, new Dictionary<string, StoryMediaType>([file]));
        }

        var message = await strategy.SendSingleAsync(chatId, inputFile, text, keyboard, ct);
        return new TelegramMessageIds(message.MessageId);
    }

    /// <summary>
    /// Delivers multiple media files as a single album (media group) followed by a text and keyboard reply.
    /// </summary>
    private async Task<TelegramMessageIds> DeliverMediaGroupAsync(
        long chatId,
        string text,
        IReadOnlyDictionary<string, StoryMediaType> files,
        InlineKeyboardMarkup keyboard,
        CancellationToken ct)
    {
        var mediaGroup = new List<IAlbumInputMedia>(files.Count);

        foreach (var kvp in files)
        {
            var inputFile = telegramMediaExtractor.GetInputFile(kvp);

            var media = GetStrategy(telegramMediaExtractor.GetMessageType(kvp)).CreateAlbumMedia(inputFile);

            mediaGroup.Add(media);
        }

        var messages = await botClient.SendMediaGroup(
            chatId: chatId,
            media: mediaGroup,
            cancellationToken: ct);

        var textMessage = await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            replyParameters: new ReplyParameters { MessageId = messages[0].MessageId },
            cancellationToken: ct);

        return new TelegramMessageIds(textMessage.MessageId, telegramMediaExtractor.ExtractMediaFiles(messages));
    }

    /// <summary>
    /// Resolves the appropriate media handling strategy based on the file's content type.
    /// </summary>
    private IMediaTypeStrategy GetStrategy(MessageType contentType)
    {
        var strategy = mediaStrategies.FirstOrDefault(s => s.CanHandle(contentType));

        return strategy ?? defaultMediaTypeStrategy;
    }
}