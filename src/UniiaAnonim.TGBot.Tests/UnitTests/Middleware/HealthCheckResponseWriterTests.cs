using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UniiaAnonim.TGBot.Api.HealthChecks;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Middleware;

public class HealthCheckResponseWriterTests
{
    /// <summary>
    /// Tests that the response writer generates a correctly formatted JSON report for a healthy status.
    /// </summary>
    [Fact]
    public async Task WriteJsonResponseHealthyStatusWritesCorrectJsonFormat()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            {
                "test-check",
                new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Everything is fine",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    null)
            },
        };

        var report = new HealthReport(entries, TimeSpan.FromSeconds(1));

        // Act
        await HealthCheckResponseWriter.WriteJsonResponse(context, report);

        // Assert
        Assert.Equal(MediaTypeNames.Application.Json, context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(HealthStatus.Healthy.ToString(), root.GetProperty("status").GetString());
        Assert.Equal(report.TotalDuration.ToString(), root.GetProperty("totalDuration").GetString());
        Assert.True(root.GetProperty("checks").GetArrayLength() > 0);

        var firstCheck = root.GetProperty("checks")[0];
        Assert.Equal("test-check", firstCheck.GetProperty("name").GetString());
        Assert.Equal(HealthStatus.Healthy.ToString(), firstCheck.GetProperty("status").GetString());
        Assert.Equal("Everything is fine", firstCheck.GetProperty("description").GetString());
        Assert.Equal(entries["test-check"].Duration.ToString(), firstCheck.GetProperty("duration").GetString());
        Assert.Null(firstCheck.GetProperty("error").GetString());
    }

    /// <summary>
    /// Tests that the response writer generates a correctly formatted JSON report including the exception message for an unhealthy status.
    /// </summary>
    [Fact]
    public async Task WriteJsonResponseUnhealthyStatusWithExceptionWritesExceptionMessageToErrorProperty()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exceptionMessage = "Database timeout error";
        var exception = new InvalidOperationException(exceptionMessage);

        var entries = new Dictionary<string, HealthReportEntry>
        {
            {
                "db-check",
                new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(500),
                    exception,
                    null)
            },
        };

        var report = new HealthReport(entries, TimeSpan.FromSeconds(2));

        // Act
        await HealthCheckResponseWriter.WriteJsonResponse(context, report);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        using var document = JsonDocument.Parse(json);
        var firstCheck = document.RootElement.GetProperty("checks")[0];

        Assert.Equal(HealthStatus.Unhealthy.ToString(), firstCheck.GetProperty("status").GetString());
        Assert.Equal(exceptionMessage, firstCheck.GetProperty("error").GetString());
    }

    /// <summary>
    /// Tests that calling the writer with a null HttpContext throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task WriteJsonResponseNullHttpContextThrowsArgumentNullException()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => HealthCheckResponseWriter.WriteJsonResponse(null, report));
    }

    /// <summary>
    /// Tests that calling the writer with a null HealthReport throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task WriteJsonResponseNullReportThrowsArgumentNullException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => HealthCheckResponseWriter.WriteJsonResponse(context, null));
    }
}