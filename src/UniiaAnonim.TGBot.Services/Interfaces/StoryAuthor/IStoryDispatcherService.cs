using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Orchestrates the dispatching of incoming anonymous stories to administrative channels.
/// </summary>
public interface IStoryDispatcherService
{
    /// <summary>
    /// Asynchronously processes the incoming story data, including any attached media files,
    /// and distributes it to the designated administrative destinations.
    /// </summary>
    /// <param name="dto">The data transfer object containing the author's Telegram ID, the story content, and optional files.</param>
    /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    Task ProcessAsync(StoryAuthorDto dto, CancellationToken ct = default);
}