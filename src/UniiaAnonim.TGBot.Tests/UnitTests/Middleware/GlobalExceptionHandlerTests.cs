using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using UniiaAnonim.TGBot.Api.Middleware;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Middleware;

/// <summary>
/// Unit tests for <see cref="GlobalExceptionHandler"/>.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly GlobalExceptionHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlerTests"/> class.
    /// </summary>
    public GlobalExceptionHandlerTests()
    {
        _problemDetailsService = Substitute.For<IProblemDetailsService>();
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _env = Substitute.For<IWebHostEnvironment>();

        _handler = new GlobalExceptionHandler(
            _problemDetailsService,
            _logger,
            _env);
    }

    /// <summary>
    /// Tests that the handler correctly maps various exception types to their corresponding HTTP status codes.
    /// </summary>
    /// <param name="exceptionType">The type of exception to simulate.</param>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    /// <param name="expectedTitle">The expected title in the problem details response.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Theory]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest, "Bad Request")]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest, "Bad Request")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status400BadRequest, "Bad Request")]
    [InlineData(typeof(NotSupportedException), StatusCodes.Status400BadRequest, "Bad Request")]
    [InlineData(typeof(Exception), StatusCodes.Status500InternalServerError, "Internal Server Error")]
    public async Task TryHandleAsyncMapsExceptionsToCorrectStatusCodes(Type exceptionType, int expectedStatusCode, string expectedTitle)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error message")!;

        _env.EnvironmentName.Returns("Production");

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);

        await _problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
            ctx.ProblemDetails.Status == expectedStatusCode &&
            ctx.ProblemDetails.Title == expectedTitle));
    }

    /// <summary>
    /// Tests that when the environment is set to 'Development', the problem details include the stack trace.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task TryHandleAsyncInDevelopmentIncludesStackTrace()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test exception");

        _env.EnvironmentName.Returns("Development");

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(true);

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        await _problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
            ctx.ProblemDetails.Extensions.ContainsKey("stackTrace") &&
            ctx.ProblemDetails.Detail == exception.Message));
    }
}