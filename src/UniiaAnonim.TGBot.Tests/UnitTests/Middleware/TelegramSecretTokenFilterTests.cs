using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using UniiaAnonim.TGBot.Api.Middleware;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Middleware;

/// <summary>
/// Unit tests for the <see cref="TelegramSecretTokenFilter"/> class.
/// </summary>
public class TelegramSecretTokenFilterTests
{
    private const string ExpectedToken = "valid-secret-token";
    private const string TelegramTokenHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    private readonly IOptions<TelegramBotOptions> _options;
    private readonly TelegramSecretTokenFilter _filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramSecretTokenFilterTests"/> class.
    /// Sets up options and the filter instance for testing.
    /// </summary>
    public TelegramSecretTokenFilterTests()
    {
        _options = Options.Create(new TelegramBotOptions { SecretToken = ExpectedToken });
        _filter = new TelegramSecretTokenFilter(_options);
    }

    /// <summary>
    /// Ensures that the filter sets an ObjectResult with 403 Forbidden and short-circuits the pipeline when the token header is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActionExecutionAsyncWhenHeaderIsMissingSetsForbiddenResultAndDoesNotCallNext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, Next);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Forbidden", objectResult.Value);
        Assert.False(nextCalled);
    }

    /// <summary>
    /// Ensures that the filter sets an ObjectResult with 403 Forbidden when the token header is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActionExecutionAsyncWhenHeaderIsInvalidSetsForbiddenResultAndDoesNotCallNext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TelegramTokenHeaderName] = "invalid-token";
        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, Next);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.False(nextCalled);
    }

    /// <summary>
    /// Ensures that the filter sets an ObjectResult with 403 Forbidden when the token header is an empty string.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActionExecutionAsyncWhenHeaderIsEmptySetsForbiddenResultAndDoesNotCallNext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TelegramTokenHeaderName] = StringValues.Empty;
        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, Next);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.False(nextCalled);
    }

    /// <summary>
    /// Ensures that the filter proceeds to call the next delegate when the token header matches the expected configuration.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActionExecutionAsyncWhenHeaderIsValidCallsNextAndDoesNotSetResult()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TelegramTokenHeaderName] = ExpectedToken;
        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, Next);

        // Assert
        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    /// <summary>
    /// Ensures that if the SecretToken is missing from configuration (null),
    /// and a header is provided, it correctly identifies a mismatch and blocks the request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActionExecutionAsyncWhenConfiguredTokenIsNullSetsForbiddenResultAndDoesNotCallNext()
    {
        // Arrange
        var emptyOptions = Options.Create(new TelegramBotOptions { SecretToken = null });
        var emptyFilter = new TelegramSecretTokenFilter(emptyOptions);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[TelegramTokenHeaderName] = "some-token";
        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        }

        // Act
        await emptyFilter.OnActionExecutionAsync(context, Next);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.False(nextCalled);
    }

    /// <summary>
    /// Helper method to create a valid <see cref="ActionExecutingContext"/> for testing.
    /// </summary>
    /// <param name="httpContext">The mocked HTTP context to use.</param>
    /// <returns>A fully initialized <see cref="ActionExecutingContext"/>.</returns>
    private static ActionExecutingContext CreateActionExecutingContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }
}