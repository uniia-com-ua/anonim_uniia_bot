using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor.MediaStrategies;

/// <summary>
/// Implements a fallback media strategy for handling document and unknown file uploads.
/// </summary>
/// <param name="client">The Telegram bot client used to interact with the API.</param>
public sealed class DocumentMediaStrategy(ITelegramBotClient client) : IDefaultMediaTypeStrategy
{
    private readonly ITelegramBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc/>
    public bool CanHandle(MessageType fileType) => true;

    /// <inheritdoc/>
    public IAlbumInputMedia CreateAlbumMedia(InputFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new InputMediaDocument(file);
    }

    /// <inheritdoc/>
    public Task<Message> SendSingleAsync(
        long chatId,
        InputFile file,
        string? caption,
        InlineKeyboardMarkup? keyboard,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        return _client.SendDocument(
            chatId: chatId,
            document: file,
            caption: caption,
            parseMode: caption is not null ? ParseMode.Html : ParseMode.None,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}