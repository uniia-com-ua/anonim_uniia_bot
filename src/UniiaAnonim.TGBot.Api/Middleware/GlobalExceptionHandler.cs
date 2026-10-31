using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Api.Middleware;

/// <summary>
/// Handles global exceptions and writes problem details responses.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env)
    : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle the exception and write a problem details response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}"/> indicating whether the exception was handled.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            StoryTooLongException or
            ValidationException or
            ArgumentException or
            NotSupportedException or
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        httpContext.Response.StatusCode = statusCode;

        var isClientError = statusCode < 500;

        if (isClientError)
        {
            LogClientErrorOccurred(logger, exception, exception.GetType().Name, exception.Message);
        }
        else
        {
            LogUnhandledServerExceptionOccurred(logger, exception, exception.GetType().Name, exception.Message);
        }

        var isDev = env.IsDevelopment();

        var detail = (isDev || isClientError)
            ? exception.Message
            : "An unexpected error occurred while processing your request. Please try again later.";

        var type = GetProblemType(exception, isDev, isClientError);

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = httpContext.Request.Path,
        };

        if (isDev)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        return await problemDetailsService.TryWriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        Exception = exception,
                        ProblemDetails = problemDetails,
                    });
    }

    private static string GetProblemType(Exception exception, bool isDev, bool isClientError)
        => isDev ? exception.GetType().Name : GetProductionProblemType(isClientError);

    private static string GetProductionProblemType(bool isClientError)
        => isClientError ? "https://tools.ietf.org/html/rfc7231#section-6.5.1" : "https://tools.ietf.org/html/rfc7231#section-6.6.1";

    [LoggerMessage(
        EventId = LogEventIds.ClientErrorOccurred,
        Level = LogLevel.Warning,
        Message = "A client error occurred while processing the request. Type: {ExceptionType}, Message: {Message}")]
    private static partial void LogClientErrorOccurred(ILogger logger, Exception exception, string exceptionType, string message);

    [LoggerMessage(
        EventId = LogEventIds.UnhandledServerExceptionOccurred,
        Level = LogLevel.Error,
        Message = "An unhandled server exception occurred while processing the request. Type: {ExceptionType}, Message: {Message}")]
    private static partial void LogUnhandledServerExceptionOccurred(ILogger logger, Exception exception, string exceptionType, string message);
}