using Microsoft.Extensions.Localization;
using UniiaAnonim.TGBot.Shared.Extensions;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Extensions;

public class LocalizationExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UnescapeWhenNullOrEmptyReturnsOriginalString(string? input)
    {
        // Act
        var result = input!.Unescape();

        // Assert
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("Line1\\nLine2", "Line1\nLine2")]
    [InlineData("Col1\\tCol2", "Col1\tCol2")]
    [InlineData("Item1\\rItem2", "Item1\rItem2")]
    [InlineData("\\\"Quoted\\\"", "\"Quoted\"")]
    [InlineData("\\'Single\\'", "'Single'")]
    [InlineData("No escapes here", "No escapes here")]
    [InlineData("Mix\\nAnd\\tMatch", "Mix\nAnd\tMatch")]
    public void UnescapeWhenContainsEscapedCharactersReturnsUnescapedString(string input, string expected)
    {
        // Act
        var result = input.Unescape();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Test\\nRegex", "Test\nRegex")]
    [InlineData("Unicode\\u00A9", "Unicode©")]
    public void UnescapeRegexWhenCalledReturnsRegexUnescapedString(string input, string expected)
    {
        // Act
        var result = input.UnescapeRegex();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UnescapedValueWhenCalledReturnsUnescapedStringFromLocalizedString()
    {
        // Arrange
        var localizedString = new LocalizedString("TestKey", "Hello\\nWorld\\t!");
        var expected = "Hello\nWorld\t!";

        // Act
        var result = localizedString.UnescapedValue();

        // Assert
        Assert.Equal(expected, result);
    }
}