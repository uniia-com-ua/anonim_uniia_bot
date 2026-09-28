using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

/// <summary>
/// Represents the message identifiers returned after delivering content to Telegram.
/// </summary>
/// <param name="InteractiveMessageId">The ID of the primary message that contains the text and inline keyboard (sent first and used for user interactions).</param>
/// <param name="MediaFiles">A dictionary containing media file IDs as keys and their corresponding file types as values.</param>
public record TelegramMessageIds(int InteractiveMessageId, Dictionary<string, StoryMediaType>? MediaFiles = null);