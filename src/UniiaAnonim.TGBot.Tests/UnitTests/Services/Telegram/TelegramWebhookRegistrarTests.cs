using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using UniiaAnonim.TGBot.Infrastructure.Services.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for the <see cref="TelegramWebhookRegistrar"/> class.
/// </summary>
public class TelegramWebhookRegistrarTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IOptions<GeneralOptions> _generalOptionsSubstitute;
    private readonly IOptions<TelegramBotOptions> _telegramOptionsSubstitute;
    private readonly TelegramWebhookRegistrar _registrar;

    public TelegramWebhookRegistrarTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _generalOptionsSubstitute = Substitute.For<IOptions<GeneralOptions>>();
        _telegramOptionsSubstitute = Substitute.For<IOptions<TelegramBotOptions>>();

        _registrar = new TelegramWebhookRegistrar(_botClientSubstitute, _generalOptionsSubstitute, _telegramOptionsSubstitute);
    }

    [Fact]
    public async Task RegisterWebhookAsyncWhenBaseUrlIsMissingThrowsInvalidOperationException()
    {
        // Arrange
        _generalOptionsSubstitute.Value.Returns(new GeneralOptions { BaseUrl = null });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _registrar.RegisterWebhookAsync(CancellationToken.None));

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("BaseUrl is missing.", invalidOperationException.Message);
    }

    [Fact]
    public async Task RegisterWebhookAsyncWhenSecretTokenIsMissingThrowsInvalidOperationException()
    {
        // Arrange
        _generalOptionsSubstitute.Value.Returns(new GeneralOptions { BaseUrl = "https://example.com" });
        _telegramOptionsSubstitute.Value.Returns(new TelegramBotOptions { SecretToken = null });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _registrar.RegisterWebhookAsync(CancellationToken.None));

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("SecretToken is missing.", invalidOperationException.Message);
    }

    [Fact]
    public async Task RegisterWebhookAsyncWhenConfigurationIsValidRegistersWebhookSuccessfully()
    {
        // Arrange
        const string baseUrl = "https://example.com";
        const string secretToken = "my-super-secret-token";
        var expectedUrl = $"{baseUrl}/{Routes.TelegramWebhook}";

        _generalOptionsSubstitute.Value.Returns(new GeneralOptions { BaseUrl = baseUrl });
        _telegramOptionsSubstitute.Value.Returns(new TelegramBotOptions { SecretToken = secretToken });

        // Act
        await _registrar.RegisterWebhookAsync(CancellationToken.None);

        // Assert
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetWebhookRequest>(req =>
                req.Url == expectedUrl &&
                req.SecretToken == secretToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterWebhookAsyncWhenBaseUrlHasTrailingSlashFormatsUrlCorrectly()
    {
        // Arrange
        const string baseUrlWithSlash = "https://example.com/";
        const string secretToken = "token";
        var expectedUrl = $"https://example.com/{Routes.TelegramWebhook}";

        _generalOptionsSubstitute.Value.Returns(new GeneralOptions { BaseUrl = baseUrlWithSlash });
        _telegramOptionsSubstitute.Value.Returns(new TelegramBotOptions { SecretToken = secretToken });

        // Act
        await _registrar.RegisterWebhookAsync(CancellationToken.None);

        // Assert
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SetWebhookRequest>(req =>
                req.Url == expectedUrl &&
                req.SecretToken == secretToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveWebhookAsyncWhenCalledRemovesWebhookSuccessfully()
    {
        // Arrange
        var ct = new CancellationTokenSource().Token;

        // Act
        await _registrar.RemoveWebhookAsync(ct);

        // Assert
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Any<DeleteWebhookRequest>(),
            ct);
    }
}