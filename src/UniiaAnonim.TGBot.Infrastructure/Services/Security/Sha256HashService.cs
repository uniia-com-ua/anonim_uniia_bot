using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Security;

/// <summary>
/// Provides hashing implementations using the SHA256 algorithm.
/// </summary>
public class Sha256HashService(
    IOptions<GeneralOptions> options)
    : IHashService
{
    private readonly string _secretKey = !string.IsNullOrWhiteSpace(options.Value.HashingKey)
        ? options.Value.HashingKey
        : throw new InvalidOperationException("HashingKey is not configured in GeneralOptions.");

    /// <inheritdoc />
    public string ComputeHash(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        var combinedBytes = Encoding.UTF8.GetBytes(_secretKey + plainText);
        var hashBytes = SHA256.HashData(combinedBytes);

        return Convert.ToBase64String(hashBytes);
    }
}