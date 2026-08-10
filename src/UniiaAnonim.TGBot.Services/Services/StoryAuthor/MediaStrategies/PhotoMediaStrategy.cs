using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor.MediaStrategies;

/// <summary>
/// Implements the media strategy for handling image uploads.
/// </summary>
/// <param name="client">The Telegram bot client used to interact with the API.</param>
public sealed class PhotoMediaStrategy(ITelegramBotClient client) : IMediaTypeStrategy
{
    private readonly ITelegramBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc/>
    public bool CanHandle(string contentType) =>
        !string.IsNullOrEmpty(contentType) &&
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IAlbumInputMedia CreateAlbumMedia(InputFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new InputMediaPhoto(file);
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

        return _client.SendPhoto(
            chatId: chatId,
            photo: file,
            caption: caption,
            parseMode: caption is not null ? ParseMode.Html : ParseMode.None,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}