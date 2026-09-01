namespace UniiaAnonim.TGBot.Application.Interfaces.Security;

/// <summary>
/// Defines methods for hashing string values using the SHA256 algorithm.
/// </summary>
public interface IHashService
{
    /// <summary>
    /// Computes the SHA256 hash of the specified plain text string.
    /// </summary>
    /// <param name="plainText">The plain text string to hash.</param>
    /// <returns>The hashed string represented in base64 format (or hex, depending on implementation).</returns>
    string ComputeHash(string plainText);
}