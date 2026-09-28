using Microsoft.AspNetCore.Builder;
using Serilog;

namespace UniiaAnonim.TGBot.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring the host.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider for the host builder.
    /// </summary>
    /// <param name="host">The <see cref="ConfigureHostBuilder"/> to configure.</param>
    public static void AddSerilog(this ConfigureHostBuilder host)
    {
        var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");

        host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsPath, "all-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5)
            .WriteTo.File(
                Path.Combine(logsPath, "errors-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error));
    }
}