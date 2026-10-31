namespace UniiaAnonim.TGBot.Shared.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a story text exceeds the maximum allowed message length.
/// </summary>
public sealed class StoryTooLongException(int currentLength, int maxLength)
    : Exception(FormatMessage(currentLength, maxLength))
{
    /// <summary>
    /// Gets the current character length of the story text.
    /// </summary>
    public int CurrentLength { get; } = currentLength;

    /// <summary>
    /// Gets the maximum allowed character limit.
    /// </summary>
    public int MaxLength { get; } = maxLength;

    private static string FormatMessage(int currentLength, int maxLength) =>
        $"Story text length ({currentLength} characters) exceeds the maximum allowed limit of {maxLength} characters.";
}