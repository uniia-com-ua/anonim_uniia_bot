using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;

namespace UniiaAnonim.TGBot.Api.Middleware;

/// <summary>
/// An asynchronous action filter that validates the secret token sent by Telegram.
/// This ensures that incoming webhook requests are genuinely from the Telegram API.
/// </summary>
/// <param name="options">The configured options containing the expected Telegram secret token.</param>
public sealed class TelegramSecretTokenFilter(IOptions<TelegramBotOptions> options)
    : IAsyncActionFilter
{
    private const string SecretTokenHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// Executes asynchronously before the action method is invoked.
    /// Validates the "X-Telegram-Bot-Api-Secret-Token" header against the configured expected token.
    /// </summary>
    /// <param name="context">The context for the executing action.</param>
    /// <param name="next">The delegate to execute the next filter or the action itself.</param>
    /// <returns>A task that represents the asynchronous filter execution.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var expectedToken = options.Value.SecretToken;

        if (!context.HttpContext.Request.Headers.TryGetValue(SecretTokenHeaderName, out var tokenValue) ||
            string.IsNullOrEmpty(expectedToken) ||
            !IsValidToken(tokenValue!, expectedToken))
        {
            context.Result = new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }

    private static bool IsValidToken(string providedToken, string expectedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}