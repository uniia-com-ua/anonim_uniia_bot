using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Application.Services.Telegram;

/// <summary>
/// Represents a service responsible for extracting media file identifiers and types from Telegram messages.
/// </summary>
public sealed class TelegramMediaProcessor : ITelegramMediaProcessor
{
    /// <inheritdoc/>
    public Dictionary<string, StoryMediaType> ExtractMediaFiles(Message message)
    {
        var mediaFiles = new Dictionary<string, StoryMediaType>(1);
        PopulateMediaFiles(message, mediaFiles);
        return mediaFiles;
    }

    /// <inheritdoc/>
    public Dictionary<string, StoryMediaType> ExtractMediaFiles(IEnumerable<Message> messages)
    {
        if (messages is null)
        {
            return [];
        }

        int capacity = messages is ICollection<Message> col ? col.Count : 4;
        var mediaFiles = new Dictionary<string, StoryMediaType>(capacity);

        foreach (var message in messages)
        {
            PopulateMediaFiles(message, mediaFiles);
        }

        return mediaFiles;
    }

    /// <inheritdoc/>
    public IAlbumInputMedia ConvertToAlbumMedia(KeyValuePair<string, StoryMediaType> mediaPair, string? caption = null)
    {
        var fileId = mediaPair.Key;

        var parseMode = caption is not null ? ParseMode.Html : ParseMode.None;

        return mediaPair.Value switch
        {
            StoryMediaType.Video => new InputMediaVideo(InputFile.FromFileId(fileId)) { Caption = caption, ParseMode = parseMode },
            StoryMediaType.Photo => new InputMediaPhoto(InputFile.FromFileId(fileId)) { Caption = caption, ParseMode = parseMode },
            StoryMediaType.Document => new InputMediaDocument(InputFile.FromFileId(fileId)) { Caption = caption, ParseMode = parseMode },
            _ => throw new ArgumentOutOfRangeException(nameof(mediaPair), $"Unsupported media type: {mediaPair.Value}"),
        };
    }

    /// <inheritdoc/>
    public List<IAlbumInputMedia> ConvertToAlbumMedia(Dictionary<string, StoryMediaType> mediaFiles, string? cleanCaption = null)
    {
        if (mediaFiles is null || mediaFiles.Count == 0)
        {
            return [];
        }

        var mediaGroup = new List<IAlbumInputMedia>(mediaFiles.Count);
        int index = 0;

        foreach (var pair in mediaFiles)
        {
            var caption = (index == 0) ? cleanCaption : null;
            mediaGroup.Add(ConvertToAlbumMedia(pair, caption));
            index++;
        }

        return mediaGroup;
    }

    /// <inheritdoc/>
    public InputFile GetInputFile(KeyValuePair<string, StoryMediaType> mediaPair)
    {
        return InputFile.FromFileId(mediaPair.Key);
    }

    /// <inheritdoc/>
    public MessageType GetMessageType(KeyValuePair<string, StoryMediaType> mediaPair)
    {
        return mediaPair.Value switch
        {
            StoryMediaType.Photo => MessageType.Photo,
            StoryMediaType.Video => MessageType.Video,
            StoryMediaType.Document => MessageType.Document,
            _ => MessageType.Document,
        };
    }

    private static void PopulateMediaFiles(Message message, Dictionary<string, StoryMediaType> destination)
    {
        if (message is null)
        {
            return;
        }

        if (message.Photo is { Length: > 0 } photos)
        {
            destination[photos[^1].FileId] = StoryMediaType.Photo;
            return;
        }

        if (message.Video?.FileId is { } videoId)
        {
            destination[videoId] = StoryMediaType.Video;
            return;
        }

        if (message.Document?.FileId is { } documentId)
        {
            destination[documentId] = StoryMediaType.Document;
            return;
        }

        if (message.Animation?.FileId is { } animationId)
        {
            destination[animationId] = StoryMediaType.Document;
            return;
        }

        if (message.Audio?.FileId is { } audioId)
        {
            destination[audioId] = StoryMediaType.Document;
        }
    }
}