using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Domain.Models;

/// <summary>
/// Represents a story author entity in the domain model.
/// </summary>
public class StoryAuthor
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public string AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the hash of the author identifier.
    /// </summary>
    public string AuthorIdHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating a story status.
    /// </summary>
    public StoryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the channel message identifier.
    /// </summary>
    public int ChannelMessageId { get; set; }

    /// <summary>
    /// Gets or sets the collection of associated Telegram message entities linked to this story.
    /// </summary>
    public ICollection<StoryFileEntity> StoryMessages { get; set; } = [];
}