namespace UniiaAnonim.TGBot.Shared.Exceptions;

/// <summary>
/// Represents an exception that is thrown when dispatching a story to administrative channels fails.
/// </summary>
public sealed class StoryDispatchFailedException(Guid storyId, long telegramId, Exception? innerException = null)
    : Exception(FormatMessage(storyId, telegramId), innerException)
{
    private static string FormatMessage(Guid storyId, long telegramId) =>
        $"Failed to dispatch story '{storyId}' for Telegram user with ID '{telegramId}'.";
}