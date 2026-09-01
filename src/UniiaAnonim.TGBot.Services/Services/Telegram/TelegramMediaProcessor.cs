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
        var mediaFiles = new Dictionary<string, StoryMediaType>();

        if (message is null)
        {
            return mediaFiles;
        }

        if (message.Photo?.OrderByDescending(p => p.FileSize).FirstOrDefault() is { FileId: { } photoId })
        {
            mediaFiles[photoId] = StoryMediaType.Photo;
            return mediaFiles;
        }

        if (message.Video?.FileId is { } videoId)
        {
            mediaFiles[videoId] = StoryMediaType.Video;
            return mediaFiles;
        }

        if (message.Document?.FileId is { } documentId)
        {
            mediaFiles[documentId] = StoryMediaType.Document;
            return mediaFiles;
        }

        if (message.Animation?.FileId is { } animationId)
        {
            mediaFiles[animationId] = StoryMediaType.Document;
            return mediaFiles;
        }

        if (message.Audio?.FileId is { } audioId)
        {
            mediaFiles[audioId] = StoryMediaType.Document;
            return mediaFiles;
        }

        return mediaFiles;
    }

    /// <inheritdoc/>
    public Dictionary<string, StoryMediaType> ExtractMediaFiles(IEnumerable<Message> messages)
    {
        var mediaFiles = new Dictionary<string, StoryMediaType>();

        if (messages is null)
        {
            return mediaFiles;
        }

        foreach (var message in messages)
        {
            foreach (var kvp in ExtractMediaFiles(message))
            {
                mediaFiles[kvp.Key] = kvp.Value;
            }
        }

        return mediaFiles;
    }

    /// <inheritdoc/>
    public IAlbumInputMedia ConvertToAlbumMedia(KeyValuePair<string, StoryMediaType> mediaPair, string? caption = null)
    {
        var fileId = mediaPair.Key;

        return mediaPair.Value switch
        {
            StoryMediaType.Video => new InputMediaVideo(InputFile.FromFileId(fileId)) { Caption = caption },
            StoryMediaType.Photo => new InputMediaPhoto(InputFile.FromFileId(fileId)) { Caption = caption },
            StoryMediaType.Document => new InputMediaDocument(InputFile.FromFileId(fileId)) { Caption = caption },
            _ => throw new NotImplementedException($"Media type '{mediaPair.Value}' is not supported."),
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
}