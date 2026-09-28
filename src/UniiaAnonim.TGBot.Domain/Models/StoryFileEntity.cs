using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Domain.Models;

/// <summary>
/// Represents a database entity that maps a story to its associated Telegram file ID.
/// </summary>
public class StoryFileEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the story.
    /// </summary>
    public Guid StoryId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram file ID associated with this story.
    /// </summary>
    public string FileId { get; set; }

    /// <summary>
    /// Gets or sets the type of the media file (e.g., Photo, Video).
    /// </summary>
    public StoryMediaType Type { get; set; }

    /// <summary>
    /// Gets or sets the associated story author entity.
    /// </summary>
    public StoryAuthor StoryAuthor { get; set; }
}