namespace UniiaAnonim.TGBot.Shared.Exceptions;

/// <summary>
/// Represents an exception that is thrown when dispatching a story to administrative channels fails.
/// </summary>
public sealed class StoryDispatchFailedException(Guid storyId, string? userHash = null, Exception? innerException = null)
    : Exception(FormatMessage(storyId, userHash), innerException)
{
    private static string FormatMessage(Guid storyId, string? userHash) =>
        userHash is null
            ? $"Failed to dispatch story '{storyId}'."
            : $"Failed to dispatch story '{storyId}' for user '{userHash}'.";
}