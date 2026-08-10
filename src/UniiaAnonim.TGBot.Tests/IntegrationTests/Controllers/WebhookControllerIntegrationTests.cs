using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Api.Controllers;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Tests.Fixtures;

namespace UniiaAnonim.TGBot.Tests.IntegrationTests.Controllers;

/// <summary>
/// HTTP-level integration tests for <see cref="WebhookController"/>.
/// </summary>
public class WebhookControllerIntegrationTests
    : IDisposable
{
    private readonly ControllerWebAppFactory<WebhookController> _factory;
    private readonly Channel<Update> _updateChannel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookControllerIntegrationTests"/> class.
    /// </summary>
    public WebhookControllerIntegrationTests()
    {
        var substituteProvider = new SubstituteProvider();

        _updateChannel = Channel.CreateUnbounded<Update>();
        substituteProvider.Register(_updateChannel);

        _factory = new ControllerWebAppFactory<WebhookController>(substituteProvider);
    }

    /// <summary>
    /// Ensures that POST /webhook returns HTTP 200 (OK) when the secret token is valid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProcessUpdateWithValidTokenReturnsOkAndQueuesUpdate()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", GetSecretToken());
        var update = new Update { Id = 123 };

        // Act
        var response = await client.PostAsJsonAsync(Routes.TelegramWebhook, update);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Ensures that POST /webhook returns HTTP 403 (Forbidden) when the secret token is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProcessUpdateWithInvalidTokenReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "wrong-token");

        var update = new Update { Id = 123 };

        // Act
        var response = await client.PostAsJsonAsync(Routes.TelegramWebhook, update);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(_updateChannel.Reader.TryRead(out _));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _factory?.Dispose();
            }

            _disposed = true;
        }
    }

    private string GetSecretToken()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        return configuration["Telegram:SecretToken"]
            ?? throw new InvalidOperationException("Telegram:SecretToken is not configured in test environment.");
    }
}