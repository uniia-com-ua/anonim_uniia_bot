using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Defines a contract for extracting media file identifiers and their types from Telegram messages.
/// </summary>
public interface ITelegramMediaProcessor
{
    /// <summary>
    /// Extracts media files (photos, videos, documents, etc.) from the specified Telegram message.
    /// </summary>
    /// <param name="message">The Telegram message containing media.</param>
    /// <returns>A dictionary containing file IDs as keys and their corresponding <see cref="StoryMediaType"/> as values, or an empty dictionary if no supported media is found.</returns>
    Dictionary<string, StoryMediaType> ExtractMediaFiles(Message message);

    /// <summary>
    /// Extracts media files (photos, videos, documents, etc.) from a collection of Telegram messages.
    /// </summary>
    /// <param name="messages">The collection of Telegram messages containing media.</param>
    /// <returns>A dictionary containing file IDs as keys and their corresponding <see cref="StoryMediaType"/> as values, or an empty dictionary if no supported media is found.</returns>
    Dictionary<string, StoryMediaType> ExtractMediaFiles(IEnumerable<Message> messages);

    /// <summary>
    /// Converts a single media key-value pair into an album input media object.
    /// </summary>
    /// <param name="mediaPair">The key-value pair containing the file ID and media type.</param>
    /// <param name="caption">The optional text caption to attach to the media.</param>
    /// <returns>An instance of <see cref="IAlbumInputMedia"/>.</returns>
    IAlbumInputMedia ConvertToAlbumMedia(KeyValuePair<string, StoryMediaType> mediaPair, string? caption = null);

    /// <summary>
    /// Converts a dictionary of media file identifiers and types into a list of album input media objects.
    /// </summary>
    /// <param name="mediaFiles">The dictionary containing file IDs and their corresponding <see cref="StoryMediaType"/>.</param>
    /// <param name="cleanCaption">The optional text caption to attach to the first media item in the group.</param>
    /// <returns>A list of <see cref="IAlbumInputMedia"/> ready to be sent as a media group.</returns>
    List<IAlbumInputMedia> ConvertToAlbumMedia(Dictionary<string, StoryMediaType> mediaFiles, string? cleanCaption = null);

    /// <summary>
    /// Creates an input file object from the specified media key-value pair.
    /// </summary>
    /// <param name="mediaPair">The key-value pair containing the file ID and media type.</param>
    /// <returns>An instance of <see cref="InputFile"/>.</returns>
    InputFile GetInputFile(KeyValuePair<string, StoryMediaType> mediaPair);

    /// <summary>
    /// Determines the Telegram message type corresponding to the media key-value pair.
    /// </summary>
    /// <param name="mediaPair">The key-value pair containing the file ID and media type.</param>
    /// <returns>An instance of <see cref="MessageType"/>.</returns>
    MessageType GetMessageType(KeyValuePair<string, StoryMediaType> mediaPair);
}