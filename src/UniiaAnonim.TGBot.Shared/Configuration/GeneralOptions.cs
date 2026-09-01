namespace UniiaAnonim.TGBot.Shared.Configuration;

/// <summary>
/// Represents the general configuration options for the application.
/// Typically bound from the "GeneralOptions" section in appsettings.json.
/// </summary>
public class GeneralOptions
{
    /// <summary>
    /// The configuration section name used to bind these options from the settings file.
    /// </summary>
    public const string Position = "GeneralOptions";

    /// <summary>
    /// Gets or sets the base URL for the application.
    /// Used for webhook configuration, callback URLs, or absolute link generation.
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the default language code (culture) for the application.
    /// E.g., "uk-UA", "en-US".
    /// </summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>
    /// Gets or sets the secret key used for symmetric encryption and data processing.
    /// </summary>
    public string SymmetricEncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key used for hashing operations.
    /// </summary>
    public string HashingKey { get; set; }
}