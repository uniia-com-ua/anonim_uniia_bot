namespace UniiaAnonim.TGBot.Shared.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a story text exceeds the maximum allowed message length.
/// </summary>
public sealed class StoryTooLongException(int currentLength, int maxLength)
    : Exception(FormatMessage(currentLength, maxLength))
{
    private static string FormatMessage(int currentLength, int maxLength) =>
        $"Story text length ({currentLength} characters) exceeds the maximum allowed limit of {maxLength} characters.";
}