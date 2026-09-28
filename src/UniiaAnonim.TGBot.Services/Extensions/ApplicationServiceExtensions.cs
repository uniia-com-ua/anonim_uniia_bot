using Microsoft.Extensions.DependencyInjection;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Application.Services.StoryAuthor;
using UniiaAnonim.TGBot.Application.Services.StoryAuthor.MediaStrategies;
using UniiaAnonim.TGBot.Application.Services.Telegram;
using UniiaAnonim.TGBot.Application.Services.Telegram.CommandStrategies;
using UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;

namespace UniiaAnonim.TGBot.Application.Extensions;

/// <summary>
/// Extension methods for configuring Application layer services in the dependency injection container.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all Application layer services.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddCoreServices()
            .AddMediaStrategies()
            .AddTelegramUpdateStrategies();
    }

    /// <summary>
    /// Registers core application services, dispatchers, and factories.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services
            .AddScoped<ITelegramUpdateDispatcher, TelegramUpdateDispatcher>()
            .AddScoped<IStoryDispatcherService, StoryDispatcherService>()
            .AddScoped<ITelegramDeliveryService, TelegramDeliveryService>()
            .AddScoped<IAdminActionKeyboardFactory, AdminActionKeyboardFactory>()
            .AddScoped<IStoryAuthorService, StoryAuthorService>()
            .AddTransient<ITelegramWebAppAuthenticator, TelegramWebAppAuthenticator>()
            .AddTransient<ITelegramMediaProcessor, TelegramMediaProcessor>();

        return services;
    }

    /// <summary>
    /// Registers media processing strategies for different types of Telegram messages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the strategies to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddMediaStrategies(this IServiceCollection services)
    {
        services
            .AddScoped<IDefaultMediaTypeStrategy, DocumentMediaStrategy>()
            .AddScoped<IMediaTypeStrategy, PhotoMediaStrategy>()
            .AddScoped<IMediaTypeStrategy, VideoMediaStrategy>();

        return services;
    }

    /// <summary>
    /// Registers all Telegram update and event processing strategies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the strategies to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance for chaining.</returns>
    private static IServiceCollection AddTelegramUpdateStrategies(this IServiceCollection services)
    {
        services
            .AddScoped<IDefaultTelegramUpdateStrategy, DefaultUpdateStrategy>()
            .AddScoped<ITelegramUpdateStrategy, WelcomeStrategy>()
            .AddScoped<ITelegramUpdateStrategy, EnsureRulesAcceptedStrategy>()
            .AddScoped<ITelegramUpdateStrategy, BotRegistrationStrategy>()
            .AddScoped<ITelegramUpdateStrategy, BotRemovalStrategy>()
            .AddScoped<ITelegramUpdateStrategy, CreateNewStoryStrategy>()
            .AddScoped<ITelegramUpdateStrategy, AdminReplyToStoryStrategy>()
            .AddScoped<ITelegramUpdateStrategy, UserReplyToAdminStrategy>()
            .AddScoped<ITelegramUpdateStrategy, RejectStoryStrategy>()
            .AddScoped<ITelegramUpdateStrategy, PublishStoryStrategy>()
            .AddScoped<ITelegramUpdateStrategy, EditStoryCallbackStrategy>()
            .AddScoped<ITelegramUpdateStrategy, AdminSubmitEditedStoryStrategy>()
            .AddScoped<ITelegramUpdateStrategy, AuthorReviewEditStrategy>()
            .AddScoped<ITelegramUpdateStrategy, AcceptRulesCallbackStrategy>();

        return services;
    }
}