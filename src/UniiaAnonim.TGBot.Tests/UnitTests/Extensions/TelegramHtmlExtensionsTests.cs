using UniiaAnonim.TGBot.Shared.Extensions;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Extensions;

/// <summary>
/// Unit tests for <see cref="TelegramHtmlExtensions"/>.
/// </summary>
public class TelegramHtmlExtensionsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void EscapeTelegramHtmlWhenNullOrEmptyReturnsEmptyString(string? input, string expected)
    {
        // Act
        var result = input.EscapeTelegramHtml();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello <World>", "Hello &lt;World&gt;")]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    [InlineData("a < b && b > c", "a &lt; b &amp;&amp; b &gt; c")]
    public void EscapeTelegramHtmlWhenContainsHtmlCharsEscapesCorrectly(string input, string expected)
    {
        // Act
        var result = input.EscapeTelegramHtml();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeTelegramHtmlWhenContainsUkrainianCharactersPreservesText()
    {
        // Arrange
        const string input = "Привіт, світ! <Це історія> & розповідь.";
        const string expected = "Привіт, світ! &lt;Це історія&gt; &amp; розповідь.";

        // Act
        var result = input.EscapeTelegramHtml();

        // Assert
        Assert.Equal(expected, result);
    }
}
