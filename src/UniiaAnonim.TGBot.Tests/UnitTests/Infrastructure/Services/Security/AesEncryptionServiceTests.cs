using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Infrastructure.Services.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Infrastructure.Services.Security;

/// <summary>
/// Unit tests for <see cref="AesEncryptionService"/>.
/// </summary>
public class AesEncryptionServiceTests
{
    private const string TestEncryptionKey = "MySuperSecretEncryptionKey_1234567890";
    private readonly AesEncryptionService _service;

    public AesEncryptionServiceTests()
    {
        var options = Options.Create(new GeneralOptions
        {
            SymmetricEncryptionKey = TestEncryptionKey,
        });

        _service = new AesEncryptionService(options);
    }

    [Fact]
    public void EncryptAndDecryptWhenValidInputRoundTripsSuccessfully()
    {
        // Arrange
        const string plainText = "987654321";

        // Act
        var cipherText = _service.Encrypt(plainText);
        var decryptedText = _service.Decrypt(cipherText);

        // Assert
        Assert.NotNull(cipherText);
        Assert.NotEqual(plainText, cipherText);
        Assert.Equal(plainText, decryptedText);
    }

    [Fact]
    public void EncryptWhenCalledMultipleTimesGeneratesRandomIV()
    {
        // Arrange
        const string plainText = "SameUserTelegramId_555";

        // Act
        var cipherText1 = _service.Encrypt(plainText);
        var cipherText2 = _service.Encrypt(plainText);

        // Assert - due to random IV, ciphertexts must differ
        Assert.NotEqual(cipherText1, cipherText2);

        // Both decrypt back to same text
        Assert.Equal(plainText, _service.Decrypt(cipherText1));
        Assert.Equal(plainText, _service.Decrypt(cipherText2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EncryptWhenNullOrEmptyThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => _service.Encrypt(invalidInput!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecryptWhenNullOrEmptyThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => _service.Decrypt(invalidInput!));
    }
}
