using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Security;

/// <summary>
/// Provides high-performance, low-allocation symmetric encryption and decryption implementations
/// using the Advanced Encryption Standard (AES) CBC algorithm with PKCS7 padding.
/// </summary>
public sealed class AesEncryptionService(
    IOptions<GeneralOptions> options)
    : ISymmetricEncryptionService
{
    private const int IvSizeInBytes = 16;
    private const int StackallocThreshold = 256;

    private readonly byte[] _keyBytes = DeriveKey(options.Value.SymmetricEncryptionKey);

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.GenerateIV();
        var iv = aes.IV;

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(plainText.Length);
        byte[]? rentedPlain = null;
        Span<byte> plainBytes = maxByteCount <= StackallocThreshold
            ? stackalloc byte[maxByteCount]
            : (rentedPlain = ArrayPool<byte>.Shared.Rent(maxByteCount));

        try
        {
            int plainWritten = Encoding.UTF8.GetBytes(plainText, plainBytes);
            int cipherLength = aes.GetCiphertextLengthCbc(plainWritten, PaddingMode.PKCS7);

            byte[] result = new byte[IvSizeInBytes + cipherLength];
            iv.CopyTo(result, 0);
            aes.EncryptCbc(plainBytes[..plainWritten], iv, result.AsSpan(IvSizeInBytes), PaddingMode.PKCS7);

            return Convert.ToBase64String(result);
        }
        finally
        {
            if (rentedPlain is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedPlain);
            }
        }
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);

        byte[] fullCipher = Convert.FromBase64String(cipherText);

        if (fullCipher.Length <= IvSizeInBytes)
        {
            throw new ArgumentException("Cipher text is invalid.", nameof(cipherText));
        }

        using var aes = Aes.Create();
        aes.Key = _keyBytes;

        ReadOnlySpan<byte> iv = fullCipher.AsSpan(0, IvSizeInBytes);
        ReadOnlySpan<byte> cipherData = fullCipher.AsSpan(IvSizeInBytes);

        byte[]? rented = null;
        Span<byte> decryptedSpan = cipherData.Length <= StackallocThreshold
            ? stackalloc byte[cipherData.Length]
            : (rented = ArrayPool<byte>.Shared.Rent(cipherData.Length));

        try
        {
            int written = aes.DecryptCbc(cipherData, iv, decryptedSpan, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(decryptedSpan[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    /// <summary>
    /// Derives a consistent 256-bit (32-byte) cryptographic key from the configured string once during initialization.
    /// </summary>
    private static byte[] DeriveKey(string? symmetricEncryptionKey)
    {
        return string.IsNullOrWhiteSpace(symmetricEncryptionKey)
            ? throw new InvalidOperationException("Symmetric encryption key is not configured in GeneralOptions.")
            : SHA256.HashData(Encoding.UTF8.GetBytes(symmetricEncryptionKey));
    }
}