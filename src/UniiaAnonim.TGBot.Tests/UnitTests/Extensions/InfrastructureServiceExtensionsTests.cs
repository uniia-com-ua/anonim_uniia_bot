using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Infrastructure.Extensions;
using UniiaAnonim.TGBot.Infrastructure.Persistence;
using UniiaAnonim.TGBot.Infrastructure.Services;
using UniiaAnonim.TGBot.Infrastructure.Services.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Extensions;

/// <summary>
/// Comprehensive unit tests for <see cref="InfrastructureServiceExtensions"/>.
/// </summary>
public class InfrastructureServiceExtensionsTests
{
    /// <summary>
    /// Tests that the infrastructure services registration successfully adds all core dependencies,
    /// including DB, Security, Repositories, and Telegram integrations.
    /// </summary>
    [Fact]
    public void AddInfrastructureServicesWithValidConfigurationRegistersAllExpectedServices()
    {
        // Arrange
        var services = CreateServices();
        var configuration = BuildValidConfiguration();

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Core & Options
        Assert.NotNull(serviceProvider.GetService<IStringLocalizerFactory>());
        Assert.NotNull(serviceProvider.GetService<IOptions<GeneralOptions>>());
        Assert.NotNull(serviceProvider.GetService<IOptions<TelegramBotOptions>>());

        // Assert - Database & Repositories
        Assert.NotNull(serviceProvider.GetService<AppDbContext>());
        Assert.NotNull(serviceProvider.GetService<IStoryAuthorRepository>());
        Assert.NotNull(serviceProvider.GetService<IChannelRepository>());

        // Assert - Security
        Assert.NotNull(serviceProvider.GetService<ISymmetricEncryptionService>());
        Assert.NotNull(serviceProvider.GetService<IHashService>());

        // Assert - Telegram Integration
        Assert.NotNull(serviceProvider.GetService<ITelegramBotClient>());
        Assert.NotNull(serviceProvider.GetService<IWebhookRegistrar>());
        Assert.NotNull(serviceProvider.GetService<Channel<Update>>());

        // Assert - Hosted Services
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, s => s is TelegramWebhookInitializer);
        Assert.Contains(hostedServices, s => s is TelegramCommandsInitializer);
        Assert.Contains(hostedServices, s => s is UpdateProcessingBackgroundService);
    }

    /// <summary>
    /// Tests that the system throws an InvalidOperationException if the database connection string is missing.
    /// </summary>
    [Fact]
    public void AddInfrastructureServicesMissingConnectionStringThrowsInvalidOperationException()
    {
        // Arrange
        var services = CreateServices();
        var overrides = new Dictionary<string, string?>
        {
            { $"ConnectionStrings:{ApplicationConsts.ConnectionStringName}", null },
        };
        var configuration = BuildValidConfiguration(overrides);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructureServices(configuration));
        Assert.Contains(ApplicationConsts.ConnectionStringName, exception.Message);
    }

    /// <summary>
    /// Tests that the Telegram Bot client is not registered when the bot token is missing or empty.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AddInfrastructureServicesWithEmptyOrNullBotTokenDoesNotRegisterTelegramClient(string? token)
    {
        // Arrange
        var services = CreateServices();
        var overrides = new Dictionary<string, string?>
        {
            { $"{TelegramBotOptions.Position}:BotToken", token },
        };
        var configuration = BuildValidConfiguration(overrides);

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Null(serviceProvider.GetService<ITelegramBotClient>());
        Assert.NotNull(serviceProvider.GetService<Channel<Update>>());
    }

    /// <summary>
    /// Tests that validating GeneralOptions fails on startup when required properties are missing.
    /// </summary>
    [Theory]
    [InlineData("BaseUrl", "")]
    [InlineData("SymmetricEncryptionKey", null)]
    [InlineData("HashingKey", "   ")]
    public void AddInfrastructureServicesInvalidGeneralOptionsThrowsOptionsValidationException(string key, string? value)
    {
        // Arrange
        var services = CreateServices();
        var overrides = new Dictionary<string, string?>
        {
            { $"{GeneralOptions.Position}:{key}", value },
        };
        var configuration = BuildValidConfiguration(overrides);

        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var options = serviceProvider.GetRequiredService<IOptions<GeneralOptions>>();

        Assert.Throws<OptionsValidationException>(() =>
        {
            _ = options.Value;
        });
    }

    /// <summary>
    /// Builds a test configuration with all required valid data to bypass validation errors.
    /// </summary>
    private static IConfiguration BuildValidConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var data = new Dictionary<string, string?>
        {
            { $"ConnectionStrings:{ApplicationConsts.ConnectionStringName}", "Host=localhost;Database=testdb;Username=postgres;Password=testpass" },
            { $"{GeneralOptions.Position}:BaseUrl", "https://example.com" },
            { $"{GeneralOptions.Position}:SymmetricEncryptionKey", "ValidEncryptionKey123" },
            { $"{GeneralOptions.Position}:HashingKey", "ValidHashingKey123" },
            { $"{TelegramBotOptions.Position}:BotToken", "12345:ValidTokenMock" },
        };

        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                data[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    /// <summary>
    /// Helper to create a service collection with base mandatory services (like Logging)
    /// required by infrastructure components (e.g., IHttpClientFactory).
    /// </summary>
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }
}