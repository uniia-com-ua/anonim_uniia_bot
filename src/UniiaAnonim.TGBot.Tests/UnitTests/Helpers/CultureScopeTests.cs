using System.Globalization;
using UniiaAnonim.TGBot.Application.Helpers;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Helpers;

/// <summary>
/// Unit tests for the <see cref="CultureScope"/> class.
/// </summary>
public class CultureScopeTests
{
    /// <summary>
    /// Ensures that initializing the scope changes the current culture and UI culture to the specified one.
    /// </summary>
    [Fact]
    public void ConstructorWithValidCultureSetsCurrentCultureAndUICulture()
    {
        // Arrange
        const string targetCultureName = "fr-FR";

        // Act
        using var scope = new CultureScope(targetCultureName);

        // Assert
        Assert.Equal(targetCultureName, CultureInfo.CurrentCulture.Name);
        Assert.Equal(targetCultureName, CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>
    /// Ensures that disposing the scope restores the original culture and UI culture.
    /// </summary>
    [Fact]
    public void DisposeWhenCalledRestoresOriginalCultures()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        const string targetCultureName = "es-ES";

        // Act
        using (var scope = new CultureScope(targetCultureName))
        {
            Assert.Equal(targetCultureName, CultureInfo.CurrentCulture.Name);
            Assert.Equal(targetCultureName, CultureInfo.CurrentUICulture.Name);
        }

        // Assert
        Assert.Equal(originalCulture, CultureInfo.CurrentCulture);
        Assert.Equal(originalUICulture, CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Ensures that passing an invalid culture name to the constructor throws a CultureNotFoundException.
    /// </summary>
    [Fact]
    public void ConstructorWithInvalidCultureThrowsCultureNotFoundException()
    {
        // Arrange
        const string invalidCultureName = "invalid-culture-name";

        // Act & Assert
        Assert.Throws<CultureNotFoundException>(() => new CultureScope(invalidCultureName));
    }
}