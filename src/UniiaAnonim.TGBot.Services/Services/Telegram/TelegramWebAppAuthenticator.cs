using System.Buffers;
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
public sealed class TelegramWebAppAuthenticator(
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

        if (string.IsNullOrEmpty(receivedHash) || string.IsNullOrEmpty(userJson) || receivedHash.Length != 64)
        {
            return false;
        }

        var dataCheckString = BuildDataCheckString(queryCollection);
        Span<byte> generatedHashBytes = stackalloc byte[32];
        ComputeSignatureHash(dataCheckString, generatedHashBytes);

        Span<byte> receivedHashBytes = stackalloc byte[32];
        try
        {
            Convert.FromHexString(receivedHash.AsSpan(), receivedHashBytes, out _, out int written);
            if (written != 32)
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(generatedHashBytes, receivedHashBytes)
            && TryExtractUserId(userJson, out telegramUserId);
    }

    /// <summary>
    /// Computes the HMAC-SHA-256 secret key once during initialization.
    /// </summary>
    private static byte[] ComputeSecretKey(string botToken) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(WebAppDataSourceKey), Encoding.UTF8.GetBytes(botToken));

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
            // Parsing failed (invalid JSON format)
        }

        return false;
    }

    /// <summary>
    /// Parses query parameters, excludes the hash field, sorts them alphabetically,
    /// and formats them into a newline-separated check string without heavy LINQ chains.
    /// </summary>
    private static string BuildDataCheckString(System.Collections.Specialized.NameValueCollection queryCollection)
    {
        var keys = queryCollection.AllKeys;
        if (keys is null || keys.Length == 0)
        {
            return string.Empty;
        }

        var sortedKeys = new List<string>(keys.Length);
        foreach (var key in keys)
        {
            if (key is not null && !string.Equals(key, HashKey, StringComparison.Ordinal))
            {
                sortedKeys.Add(key);
            }
        }

        sortedKeys.Sort(StringComparer.Ordinal);

        var sb = new StringBuilder(sortedKeys.Count * 32);
        for (int i = 0; i < sortedKeys.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }

            var key = sortedKeys[i];
            sb.Append(key).Append('=').Append(queryCollection[key]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes the HMAC-SHA-256 signature for the data check string using the precomputed secret key.
    /// </summary>
    private void ComputeSignatureHash(string dataCheckString, Span<byte> destination)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(dataCheckString.Length);
        if (maxBytes <= 512)
        {
            Span<byte> dataBytes = stackalloc byte[maxBytes];
            int written = Encoding.UTF8.GetBytes(dataCheckString, dataBytes);
            HMACSHA256.HashData(_secretKey, dataBytes[..written], destination);
        }
        else
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(maxBytes);
            try
            {
                int written = Encoding.UTF8.GetBytes(dataCheckString, rented);
                HMACSHA256.HashData(_secretKey, rented.AsSpan(0, written), destination);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}