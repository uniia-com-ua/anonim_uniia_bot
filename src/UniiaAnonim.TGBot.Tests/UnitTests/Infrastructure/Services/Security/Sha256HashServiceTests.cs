using Microsoft.Extensions.Options;
using UniiaAnonim.TGBot.Infrastructure.Services.Security;
using UniiaAnonim.TGBot.Shared.Configuration;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Infrastructure.Services.Security;

/// <summary>
/// Unit tests for <see cref="Sha256HashService"/>.
/// </summary>
public class Sha256HashServiceTests
{
    private const string TestHashingKey = "MySuperSecretHashingKey_12345";
    private readonly Sha256HashService _service;

    public Sha256HashServiceTests()
    {
        var options = Options.Create(new GeneralOptions
        {
            HashingKey = TestHashingKey,
        });

        _service = new Sha256HashService(options);
    }

    [Fact]
    public void ComputeHashWhenValidInputReturnsConsistentHash()
    {
        // Arrange
        const string input = "123456789";

        // Act
        var hash1 = _service.ComputeHash(input);
        var hash2 = _service.ComputeHash(input);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHashWhenDifferentInputsReturnsDifferentHashes()
    {
        // Arrange
        const string input1 = "user_1";
        const string input2 = "user_2";

        // Act
        var hash1 = _service.ComputeHash(input1);
        var hash2 = _service.ComputeHash(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ComputeHashWhenNullOrEmptyThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => _service.ComputeHash(invalidInput!));
    }
}
