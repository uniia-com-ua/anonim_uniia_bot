using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Application.Interfaces.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Infrastructure.Services.Security;

/// <summary>
/// Provides symmetric encryption and decryption implementations using the Advanced Encryption Standard (AES) algorithm.
/// </summary>
public class AesEncryptionService(
    IOptions<GeneralOptions> options)
    : ISymmetricEncryptionService
{
    private readonly byte[] _keyBytes = DeriveKey(options.Value.SymmetricEncryptionKey);

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.GenerateIV();

        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();

        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _keyBytes;

        var iv = new byte[aes.BlockSize / 8];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        return sr.ReadToEnd();
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