using Microsoft.AspNetCore.Mvc;
using UniiaAnonim.TGBot.Api.Middleware;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Api.Controllers;

/// <summary>
/// Controller responsible for receiving anonymous stories from the Telegram Web App.
/// </summary>
[Route(Routes.StoryRoute)]
[ApiController]
[ServiceFilter(typeof(TelegramWebAppAuthFilter))]
public class StoriesController(IStoryDispatcherService storyDispatcherService) : ControllerBase
{
    private const string TelegramUserIdKey = TelegramWebAppAuthFilter.TelegramUserIdKey;

    /// <summary>
    /// Processes an incoming story submission from the Web App and dispatches it to admin channels.
    /// </summary>
    /// <param name="request">The story payload including text and optional media files.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A status code indicating the successful receipt and processing of the story.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitStoryAsync(
        [FromForm] CreateStoryAuthorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetTelegramUserId(out var telegramUserId))
        {
            return Forbid();
        }

        await storyDispatcherService.ProcessAsync(
            new(telegramUserId, request.Story, request.Files),
            cancellationToken);

        return Ok();
    }

    private bool TryGetTelegramUserId(out long telegramUserId)
    {
        telegramUserId = default;

        if (!HttpContext.Items.TryGetValue(TelegramUserIdKey, out var userIdObj) ||
            userIdObj is not long id)
        {
            return false;
        }

        telegramUserId = id;
        return true;
    }
}