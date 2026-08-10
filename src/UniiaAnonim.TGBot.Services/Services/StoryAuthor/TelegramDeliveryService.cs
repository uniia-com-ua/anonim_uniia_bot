using Microsoft.AspNetCore.Http;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
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
        IReadOnlyList<IFormFile>? files,
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

        var streamsToDispose = new List<Stream>(files.Count);
        try
        {
            return files.Count == 1
                ? await DeliverSingleFileAsync(chatId, text, files[0], keyboard, isLongText, streamsToDispose, ct)
                : await DeliverMediaGroupAsync(chatId, text, files, keyboard, streamsToDispose, ct);
        }
        finally
        {
            foreach (var stream in streamsToDispose)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Delivers a single media file to the chat.
    /// </summary>
    private async Task<TelegramMessageIds> DeliverSingleFileAsync(
        long chatId,
        string text,
        IFormFile file,
        InlineKeyboardMarkup keyboard,
        bool isLongText,
        List<Stream> streamsToDispose,
        CancellationToken ct)
    {
        var stream = file.OpenReadStream();

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        streamsToDispose.Add(stream);

        string fileName = string.IsNullOrWhiteSpace(file.FileName) ? "media_file" : file.FileName;
        var inputFile = InputFile.FromStream(stream, fileName);

        var strategy = GetStrategy(file.ContentType);

        if (isLongText)
        {
            var mediaMessage = await strategy.SendSingleAsync(chatId, inputFile, caption: null, keyboard: null, ct);

            var mediaFiles = telegramMediaExtractor.ExtractMediaFiles(mediaMessage);

            if (mediaFiles.Count == 0)
            {
                throw new FileNotFoundException("File is not found while trying to save message");
            }

            var followUpMessage = await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                replyParameters: new ReplyParameters { MessageId = mediaMessage.MessageId },
                cancellationToken: ct);

            return new TelegramMessageIds(followUpMessage.MessageId, mediaFiles);
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
        IReadOnlyList<IFormFile> files,
        InlineKeyboardMarkup keyboard,
        List<Stream> streamsToDispose,
        CancellationToken ct)
    {
        var mediaGroup = new List<IAlbumInputMedia>(files.Count);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var stream = file.OpenReadStream();

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            streamsToDispose.Add(stream);

            string fileName = string.IsNullOrWhiteSpace(file.FileName) ? $"media_file_{i}" : file.FileName;
            var inputFile = InputFile.FromStream(stream, fileName);

            var media = GetStrategy(file.ContentType).CreateAlbumMedia(inputFile);

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
    private IMediaTypeStrategy GetStrategy(string contentType)
    {
        var strategy = mediaStrategies.FirstOrDefault(s => s.CanHandle(contentType));

        return strategy ?? defaultMediaTypeStrategy;
    }
}