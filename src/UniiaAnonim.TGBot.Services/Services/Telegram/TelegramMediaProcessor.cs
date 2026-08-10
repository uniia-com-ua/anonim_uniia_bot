using Telegram.Bot.Types;
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
            mediaFiles[documentId] = StoryMediaType.Photo;
            return mediaFiles;
        }

        if (message.Animation?.FileId is { } animationId)
        {
            mediaFiles[animationId] = StoryMediaType.Video;
            return mediaFiles;
        }

        if (message.Audio?.FileId is { } audioId)
        {
            mediaFiles[audioId] = StoryMediaType.Photo;
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
            var extractedFiles = ExtractMediaFiles(message);
            foreach (var kvp in extractedFiles)
            {
                mediaFiles[kvp.Key] = kvp.Value;
            }
        }

        return mediaFiles;
    }

    /// <inheritdoc/>
    public List<IAlbumInputMedia> ConvertToAlbumMedia(Dictionary<string, StoryMediaType> mediaFiles, string? cleanCaption = null)
    {
        var mediaGroup = new List<IAlbumInputMedia>();

        if (mediaFiles is null || mediaFiles.Count == 0)
        {
            return mediaGroup;
        }

        int index = 0;
        foreach (var pair in mediaFiles)
        {
            var fileId = pair.Key;
            var mediaType = pair.Value;
            var caption = (index == 0) ? cleanCaption : null;

            IAlbumInputMedia mediaItem = mediaType switch
            {
                StoryMediaType.Video => new InputMediaVideo(InputFile.FromFileId(fileId)) { Caption = caption },
                StoryMediaType.Photo => new InputMediaPhoto(InputFile.FromFileId(fileId)) { Caption = caption },
                _ => throw new NotImplementedException(),
            };

            mediaGroup.Add(mediaItem);
            index++;
        }

        return mediaGroup;
    }
}