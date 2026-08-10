using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Defines a strategy for handling specific media types when sending files via Telegram.
/// </summary>
public interface IMediaTypeStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the specified MIME content type.
    /// </summary>
    /// <param name="contentType">The MIME content type of the file (e.g., "image/jpeg").</param>
    /// <returns><see langword="true"/> if the strategy can handle the content type; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(string contentType);

    /// <summary>
    /// Creates an album input media object suitable for sending in a Telegram media group.
    /// </summary>
    /// <param name="file">The input file to be wrapped in a media object.</param>
    /// <returns>An instance of <see cref="IAlbumInputMedia"/>.</returns>
    IAlbumInputMedia CreateAlbumMedia(InputFile file);

    /// <summary>
    /// Asynchronously sends a single media file to the specified Telegram chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the target chat.</param>
    /// <param name="file">The media file to send.</param>
    /// <param name="caption">The optional text caption to attach to the media.</param>
    /// <param name="keyboard">The optional inline keyboard markup to attach to the message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing the sent <see cref="Message"/>.</returns>
    Task<Message> SendSingleAsync(long chatId, InputFile file, string? caption, InlineKeyboardMarkup? keyboard, CancellationToken ct);
}