using System.Buffers;
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

    private readonly byte[] _expectedTokenBytes = Encoding.UTF8.GetBytes(options.Value.SecretToken ?? string.Empty);

    /// <summary>
    /// Executes asynchronously before the action method is invoked.
    /// Validates the "X-Telegram-Bot-Api-Secret-Token" header against the configured expected token.
    /// </summary>
    /// <param name="context">The context for the executing action.</param>
    /// <param name="next">The delegate to execute the next filter or the action itself.</param>
    /// <returns>A task that represents the asynchronous filter execution.</returns>
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (_expectedTokenBytes.Length == 0 ||
            !context.HttpContext.Request.Headers.TryGetValue(SecretTokenHeaderName, out var tokenValue) ||
            string.IsNullOrEmpty(tokenValue) ||
            !IsValidToken(tokenValue!))
        {
            context.Result = new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            return Task.CompletedTask;
        }

        return next();
    }

    private bool IsValidToken(string providedToken)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(providedToken.Length);
        if (maxBytes <= 256)
        {
            Span<byte> providedBytes = stackalloc byte[maxBytes];
            int written = Encoding.UTF8.GetBytes(providedToken, providedBytes);

            return written == _expectedTokenBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(providedBytes[..written], _expectedTokenBytes);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            int written = Encoding.UTF8.GetBytes(providedToken, rented);
            return written == _expectedTokenBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(rented.AsSpan(0, written), _expectedTokenBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}