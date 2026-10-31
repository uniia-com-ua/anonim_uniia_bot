using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Security;

/// <summary>
/// Provides high-performance, low-allocation hashing implementations using the SHA256 algorithm.
/// </summary>
public sealed class Sha256HashService(
    IOptions<GeneralOptions> options)
    : IHashService
{
    private const int StackallocThreshold = 256;

    private readonly byte[] _secretKeyBytes = Encoding.UTF8.GetBytes(
        !string.IsNullOrWhiteSpace(options.Value.HashingKey)
            ? options.Value.HashingKey
            : throw new InvalidOperationException("HashingKey is not configured in GeneralOptions."));

    /// <inheritdoc />
    public string ComputeHash(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        int keyLength = _secretKeyBytes.Length;
        int maxPlainTextBytes = Encoding.UTF8.GetMaxByteCount(plainText.Length);
        int totalMaxBytes = keyLength + maxPlainTextBytes;

        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];

        if (totalMaxBytes <= StackallocThreshold)
        {
            Span<byte> buffer = stackalloc byte[totalMaxBytes];
            _secretKeyBytes.CopyTo(buffer);
            int written = Encoding.UTF8.GetBytes(plainText, buffer[keyLength..]);
            SHA256.HashData(buffer[..(keyLength + written)], hashBytes);
        }
        else
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(totalMaxBytes);
            try
            {
                _secretKeyBytes.CopyTo(rented, 0);
                int written = Encoding.UTF8.GetBytes(plainText, rented.AsSpan(keyLength));
                SHA256.HashData(rented.AsSpan(0, keyLength + written), hashBytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        return Convert.ToBase64String(hashBytes);
    }
}