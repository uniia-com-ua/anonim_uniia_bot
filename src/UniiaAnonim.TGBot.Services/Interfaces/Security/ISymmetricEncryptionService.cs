namespace UniiaAnonim.TGBot.Application.Interfaces.Security;

/// <summary>
/// Defines methods for symmetric encryption and decryption of string values.
/// </summary>
public interface ISymmetricEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text string using symmetric encryption.
    /// </summary>
    /// <param name="plainText">The plain text string to encrypt.</param>
    /// <returns>The encrypted string represented in base64 format.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts the specified encrypted base64 string back to its original plain text form.
    /// </summary>
    /// <param name="cipherText">The encrypted string in base64 format to decrypt.</param>
    /// <returns>The original plain text string.</returns>
    string Decrypt(string cipherText);
}