using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Infrastructure.HealthChecks;
using UniiaAnonim.TGBot.Infrastructure.Persistence;
using UniiaAnonim.TGBot.Infrastructure.Repositories;
using UniiaAnonim.TGBot.Infrastructure.Services;
using UniiaAnonim.TGBot.Infrastructure.Services.Security;
using UniiaAnonim.TGBot.Infrastructure.Services.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register infrastructure-level services,
/// including database repositories, third-party clients, and background processors.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all required infrastructure services into the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddGeneralOptions(configuration)
            .AddDatabase(configuration)
            .AddCoreInfrastructure()
            .AddSecurity()
            .AddRepositories()
            .AddTelegramIntegration(configuration)
            .AddInfrastructureHealthChecks();

    /// <summary>
    /// Applies any pending Entity Framework core migrations to the database upon application startup,
    /// ensuring the database schema is fully up-to-date.
    /// </summary>
    /// <param name="app">The application service provider / host.</param>
    /// <returns>The original <see cref="IServiceProvider"/> instance for chaining.</returns>
    public static async Task<IServiceProvider> UseInfrastructureDatabaseAsync(this IServiceProvider app)
    {
        using var scope = app.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        return app;
    }

    /// <summary>
    /// Configures general application options from the configuration section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the options to.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddGeneralOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<GeneralOptions>()
            .Bind(configuration.GetSection(GeneralOptions.Position))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), "GeneralOptions: BaseUrl is required and cannot be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SymmetricEncryptionKey), "GeneralOptions: SymmetricEncryptionKey is required and cannot be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.HashingKey), "GeneralOptions: HashingKey is required and cannot be empty.")
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers the PostgreSQL database context using the configured connection string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ApplicationConsts.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ApplicationConsts.ConnectionStringName}' not found in configuration.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Registers security and cryptographic infrastructure services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services
            .AddScoped<ISymmetricEncryptionService, AesEncryptionService>()
            .AddScoped<IHashService, Sha256HashService>();

        return services;
    }

    /// <summary>
    /// Registers core infrastructure services like Localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddCoreInfrastructure(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = ResourcesConsts.ResourceFolder);
        return services;
    }

    /// <summary>
    /// Registers the infrastructure-specific implementations of domain repositories.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<IStoryAuthorRepository, StoryAuthorRepository>()
            .AddScoped<IChannelRepository, ChannelRepository>();

        return services;
    }

    /// <summary>
    /// Registers Telegram-related services, clients, queues, and background workers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddTelegramIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TelegramBotOptions.Position);
        services.Configure<TelegramBotOptions>(section);

        var options = section.Get<TelegramBotOptions>();

        if (!string.IsNullOrEmpty(options?.BotToken))
        {
            services.AddHttpClient(ApplicationConsts.TelegramHttpClientName)
                .AddTypedClient<ITelegramBotClient>((httpClient, _) => new TelegramBotClient(options.BotToken, httpClient))
                .AddStandardResilienceHandler();
        }

        services.AddScoped<IWebhookRegistrar, TelegramWebhookRegistrar>();

        var channel = Channel.CreateUnbounded<Update>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        services.AddSingleton(channel);
        services.AddSingleton(channel.Writer);
        services.AddSingleton(channel.Reader);

        services
            .AddHostedService<TelegramWebhookInitializer>()
            .AddHostedService<TelegramCommandsInitializer>()
            .AddHostedService<UpdateProcessingBackgroundService>();

        return services;
    }

    private static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<TelegramHealthCheck>(HealthCheckNames.Telegram)
            .AddDbContextCheck<AppDbContext>(HealthCheckNames.DbContext);

        return services;
    }
}