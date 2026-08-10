using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;

namespace UniiaAnonim.TGBot.Api.Middleware;

/// <summary>
/// An asynchronous action filter that validates the Telegram Web App initialization data
/// and securely stores the authenticated user's Telegram ID in the current HttpContext.
/// </summary>
/// <param name="authenticator">The service used to validate the initData string and extract the user ID.</param>
public sealed class TelegramWebAppAuthFilter(ITelegramWebAppAuthenticator authenticator)
    : IAsyncActionFilter
{
    public const string TelegramUserIdKey = "TelegramUserId";
    private const string InitDataHeaderName = "X-Telegram-Init-Data";

    /// <summary>
    /// Executes asynchronously before the action method is invoked.
    /// Validates the "X-Telegram-Init-Data" header. If valid, extracts the User ID and saves it.
    /// </summary>
    /// <param name="context">The context for the executing action.</param>
    /// <param name="next">The delegate to execute the next filter or the action itself.</param>
    /// <returns>A task that represents the asynchronous filter execution.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.HttpContext.Request.Headers.TryGetValue(InitDataHeaderName, out var initDataValue) ||
            !authenticator.TryValidateAndExtractUserId(initDataValue.ToString(), out var telegramUserId))
        {
            context.Result = new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        context.HttpContext.Items[TelegramUserIdKey] = telegramUserId;

        await next();
    }
}