using UniiaAnonim.TGBot.Api.Middleware;

namespace UniiaAnonim.TGBot.Api.Extensions;

/// <summary>
/// Extension methods for configuring Presentation layer services in the dependency injection container.
/// </summary>
public static class PresentationExtensions
{
    /// <summary>
    /// Registers Presentation layer services, including controllers, swagger, health checks, and global exception handling.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddTransient<TelegramSecretTokenFilter>();
        services.AddTransient<TelegramWebAppAuthFilter>();

        return services;
    }
}