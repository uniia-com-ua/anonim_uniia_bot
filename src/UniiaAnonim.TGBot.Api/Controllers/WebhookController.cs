using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Api.Middleware;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Api.Controllers;

/// <summary>
/// Controller responsible for receiving webhook updates from the Telegram API.
/// </summary>
[Route(Routes.TelegramWebhook)]
[ApiController]
[ServiceFilter(typeof(TelegramSecretTokenFilter))]
public class WebhookController(ChannelWriter<Update> updateWriter) : ControllerBase
{
    /// <summary>
    /// Processes incoming updates from Telegram and queues them for background processing.
    /// </summary>
    /// <param name="update">The update payload received from the Telegram API.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A status code indicating the successful receipt of the update.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ProcessUpdateAsync(
        [FromBody] Update update,
        CancellationToken cancellationToken)
    {
        await updateWriter.WriteAsync(update, cancellationToken);

        return Ok();
    }
}