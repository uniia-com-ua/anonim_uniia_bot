using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace UniiaAnonim.TGBot.Shared.Extensions;

/// <summary>
/// Provides extension methods for strings to handle localization-specific text formatting,
/// such as unescaping characters that were mistakenly double-escaped during resource compilation.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Converts escaped character sequences (like "\\n", "\\r", "\\t") in a string
    /// back into their actual character representations (like "\n", "\r", "\t").
    /// </summary>
    /// <param name="text">The string containing potentially double-escaped characters.</param>
    /// <returns>A string with properly rendered control characters.</returns>
    /// <example>
    /// <code>
    /// string input = "Line one\\nLine two";
    /// string result = input.Unescape(); // "Line one\nLine two"
    /// </code>
    /// </example>
    public static string Unescape(this string text)
    {
        return string.IsNullOrEmpty(text)
            ? text
            : text
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\'", "'");
    }

    /// <summary>
    /// A more robust version of <see cref="Unescape"/> using Regex,
    /// which handles all standard C# escape sequences dynamically.
    /// </summary>
    /// <param name="text">The string to unescape.</param>
    /// <returns>A string with interpreted escape sequences.</returns>
    public static string UnescapeRegex(this string text)
    {
        return Regex.Unescape(text);
    }

    /// <summary>
    /// Extension to extract the localized string value and unescape special characters.
    /// </summary>
    /// <param name="localizedString">The LocalizedString instance from IStringLocalizer.</param>
    /// <returns>The unescaped string value.</returns>
    public static string UnescapedValue(this LocalizedString localizedString)
    {
        return localizedString.Value.Unescape();
    }
}