using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration.Telegram;

namespace UniiaAnonim.TGBot.Application.Services.Telegram;

/// <summary>
/// Implements cryptographic validation for Telegram Web App initialization data (<c>initData</c>)
/// and extracts the authenticated Telegram user ID according to official Telegram specifications.
/// </summary>
public class TelegramWebAppAuthenticator(
    IOptions<TelegramBotOptions> options)
    : ITelegramWebAppAuthenticator
{
    private const string WebAppDataSourceKey = "WebAppData";
    private const string HashKey = "hash";
    private const string UserKey = "user";

    private readonly byte[] _secretKey = ComputeSecretKey(options.Value.BotToken);

    /// <inheritdoc/>
    public bool TryValidateAndExtractUserId(string? initData, out long telegramUserId)
    {
        telegramUserId = 0;

        if (string.IsNullOrEmpty(initData))
        {
            return false;
        }

        var queryCollection = HttpUtility.ParseQueryString(initData);
        var receivedHash = queryCollection[HashKey];
        var userJson = queryCollection[UserKey];

        if (string.IsNullOrEmpty(receivedHash) || string.IsNullOrEmpty(userJson))
        {
            return false;
        }

        var dataCheckString = BuildDataCheckString(queryCollection);
        var generatedHash = ComputeSignatureHash(dataCheckString);

        return VerifyHashesMatch(generatedHash, receivedHash) && TryExtractUserId(userJson, out telegramUserId);
    }

    /// <summary>
    /// Computes the HMAC-SHA-256 secret key once during initialization.
    /// </summary>
    private static byte[] ComputeSecretKey(string botToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebAppDataSourceKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
    }

    /// <summary>
    /// Parses the JSON user object to extract the Telegram User ID.
    /// </summary>
    private static bool TryExtractUserId(string userJson, out long userId)
    {
        userId = 0;
        try
        {
            using var doc = JsonDocument.Parse(userJson);
            if (doc.RootElement.TryGetProperty("id", out var idElement) && idElement.TryGetInt64(out var id))
            {
                userId = id;
                return true;
            }
        }
        catch (JsonException)
        {
            // Ignored, parsing failed (invalid JSON format)
        }

        return false;
    }

    /// <summary>
    /// Parses query parameters, excludes the hash field, sorts them alphabetically,
    /// and formats them into a newline-separated check string.
    /// </summary>
    private static string BuildDataCheckString(System.Collections.Specialized.NameValueCollection queryCollection)
    {
        var formattedParams = queryCollection.AllKeys
                .Where(key => key is not null and not HashKey)
                .OrderBy(key => key, StringComparer.Ordinal)
                .Select(key => $"{key}={queryCollection[key]}");

        return string.Join("\n", formattedParams);
    }

    /// <summary>
    /// Computes the HMAC-SHA-256 signature for the data check string using the precomputed secret key.
    /// </summary>
    private string ComputeSignatureHash(string dataCheckString)
    {
        byte[] generatedHashBytes;
        using (var hmac = new HMACSHA256(_secretKey))
        {
            generatedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        }

        return Convert.ToHexString(generatedHashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Safely compares the generated hash with the received hash in constant time to prevent timing attacks.
    /// </summary>
    private static bool VerifyHashesMatch(string generatedHash, string receivedHash)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(generatedHash),
            Encoding.UTF8.GetBytes(receivedHash.ToLowerInvariant()));
    }
}