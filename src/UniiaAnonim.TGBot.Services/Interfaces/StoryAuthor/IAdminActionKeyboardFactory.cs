using Telegram.Bot.Types.ReplyMarkups;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Defines a factory for creating inline keyboards used by administrators to moderate stories.
/// </summary>
public interface IAdminActionKeyboardFactory
{
    /// <summary>
    /// Creates an inline keyboard with moderation actions (Publish, Edit, Reject) for a specific story.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story to moderate.</param>
    /// <returns>An <see cref="InlineKeyboardMarkup"/> containing the moderation buttons.</returns>
    InlineKeyboardMarkup CreateModerationKeyboard(Guid storyId);
}