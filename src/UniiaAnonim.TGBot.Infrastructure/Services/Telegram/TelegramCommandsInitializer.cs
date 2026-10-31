using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Helpers;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Telegram;

/// <summary>
/// Background service that runs on application startup to register global Telegram bot commands,
/// applying localization to command descriptions.
/// </summary>
/// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
/// <param name="localizer">The localizer used to retrieve translated command descriptions.</param>
/// <param name="generalOptions">The general configuration options.</param>
/// <param name="logger">The logger used to record configuration status and errors.</param>
public sealed partial class TelegramCommandsInitializer(
    ITelegramBotClient botClient,
    IStringLocalizer<Messages> localizer,
    IOptions<GeneralOptions> generalOptions,
    ILogger<TelegramCommandsInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        LogSettingUpGlobalBotCommands(logger);

        try
        {
            using var scope = new CultureScope(generalOptions.Value.DefaultLanguage ?? ApplicationConsts.DefaultLanguage);

            await botClient.SetMyCommands(
                commands: [.. TelegramBotCommands.GroupChatsCommands.Select(c => new BotCommand(c.Command, localizer[c.DescriptionKey]))],
                scope: BotCommandScope.AllGroupChats(),
                cancellationToken: cancellationToken);

            await botClient.SetMyCommands(
                commands: [.. TelegramBotCommands.UsersCommands.Select(c => new BotCommand(c.Command, localizer[c.DescriptionKey]))],
                scope: BotCommandScope.AllPrivateChats(),
                cancellationToken: cancellationToken);

            LogGlobalBotCommandsConfiguredSuccessfully(logger);
        }
        catch (Exception ex)
        {
            LogFailedToSetGlobalBotCommands(logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = LogEventIds.SettingUpGlobalBotCommands,
        Level = LogLevel.Information,
        Message = "Setting up global Telegram bot commands...")]
    private static partial void LogSettingUpGlobalBotCommands(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.GlobalBotCommandsConfiguredSuccessfully,
        Level = LogLevel.Information,
        Message = "Successfully configured global bot commands with localization.")]
    private static partial void LogGlobalBotCommandsConfiguredSuccessfully(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.FailedToSetGlobalBotCommands,
        Level = LogLevel.Error,
        Message = "Failed to set global bot commands on startup.")]
    private static partial void LogFailedToSetGlobalBotCommands(ILogger logger, Exception exception);
}