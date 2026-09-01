namespace UniiaAnonim.TGBot.Shared.Configuration.Telegram;

/// <summary>
/// Represents the configuration options required for Telegram Bot integration.
/// Typically bound from the application configuration (e.g., appsettings.json).
/// </summary>
public class TelegramBotOptions
{
    /// <summary>
    /// The configuration section name used to bind these options from the settings file.
    /// </summary>
    public const string Position = "Telegram";

    /// <summary>
    /// Gets or sets the unique authorization token for the Telegram Bot.
    /// This token is obtained from the BotFather on Telegram (e.g., "123456789:ABCdefGHIjklmNOPQrsTUVwxyZ").
    /// </summary>
    public string BotToken { get; set; }

    /// <summary>
    /// Gets or sets the secret token used to verify that incoming webhook requests
    /// genuinely originate from the Telegram API.
    /// </summary>
    /// <remarks>
    /// This token is sent as the "X-Telegram-Bot-Api-Secret-Token" header in every webhook request.
    /// It is highly recommended to validate this header to prevent spoofing attacks.
    /// </remarks>
    public string? SecretToken { get; set; }
}