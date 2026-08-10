namespace UniiaAnonim.TGBot.Application.Interfaces.Telegram;

/// <summary>
/// Authenticator service for validating Telegram Web App initialization data
/// and extracting the authenticated user's Telegram ID.
/// </summary>
public interface ITelegramWebAppAuthenticator
{
    /// <summary>
    /// Validates the provided initialization data string and extracts the Telegram user ID if successful.
    /// </summary>
    /// <param name="initData">The initialization data received from the Telegram Web App.</param>
    /// <param name="telegramUserId">When this method returns, contains the extracted Telegram User ID if validation succeeded; otherwise, 0.</param>
    /// <returns>
    /// <see langword="true"/> if the data is valid and the user ID is extracted; otherwise, <see langword="false"/>.
    /// </returns>
    bool TryValidateAndExtractUserId(string? initData, out long telegramUserId);
}