using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniiaAnonim.TGBot.Tests.Fixtures;

/// <summary>
/// A custom <see cref="WebApplicationFactory{T}"/> for controller integration tests,
/// allowing injection of substituted dependencies via a <see cref="SubstituteProvider"/>.
/// </summary>
/// <typeparam name="T">
/// The entry point type of the ASP.NET Core application.
/// Typically this is the <c>Program</c> class used to bootstrap the Web API.
/// </typeparam>
/// <remarks>
/// This factory replaces registered services with substitutes from the provided <see cref="SubstituteProvider"/>.
/// It is intended for use in integration or functional tests where controller dependencies
/// need to be substituted while still exercising the real ASP.NET Core pipeline.
/// </remarks>
public class ControllerWebAppFactory<T>(SubstituteProvider substituteProvider) : WebApplicationFactory<T>
    where T : ControllerBase
{
    /// <summary>
    /// Initializes static members of the <see cref="ControllerWebAppFactory{T}"/> class.
    /// Provides the minimal configuration required by the real <c>Program</c> startup.
    /// Set as environment variables so they are picked up by <c>WebApplication.CreateBuilder</c>
    /// at configuration time, before services are registered (the in-memory hosting model reads
    /// configuration earlier than <see cref="IWebHostBuilder"/> callbacks run).
    /// The bot token must match Telegram's expected format (<c>&lt;numericId&gt;:&lt;string&gt;</c>),
    /// otherwise <c>TelegramBotClient</c> rejects it during dependency resolution.
    /// </summary>
    static ControllerWebAppFactory()
    {
        Environment.SetEnvironmentVariable("Telegram__BotToken", "123456:test-bot-token-for-integration-tests");
        Environment.SetEnvironmentVariable("Telegram__SecretToken", "test-secret-token");
    }

    /// <summary>
    /// Gets the <see cref="SubstituteProvider"/> containing all registered substitutes for dependency injection.
    /// </summary>
    public SubstituteProvider SubstituteProvider { get; } = substituteProvider;

    /// <summary>
    /// Configures the web host to replace services with substitutes from the <see cref="SubstituteProvider"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/> to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            foreach (var pair in SubstituteProvider)
            {
                services.RemoveAll(pair.Key);
                services.AddSingleton(pair.Key, pair.Value);
            }
        });
    }
}