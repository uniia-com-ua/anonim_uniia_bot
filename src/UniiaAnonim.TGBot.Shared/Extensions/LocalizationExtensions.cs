using System.Text;
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
    /// Converts escaped character sequences (like "\\n", "\\r", "\\t", "\\\"", "\\'") in a string
    /// back into their actual character representations in a single high-performance pass.
    /// Uses a zero-allocation fast-path when no backslash character is detected.
    /// </summary>
    /// <param name="text">The string containing potentially double-escaped characters.</param>
    /// <returns>A string with properly rendered control characters.</returns>
    public static string Unescape(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        int firstBackslash = text.IndexOf('\\');
        if (firstBackslash < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        sb.Append(text.AsSpan(0, firstBackslash));

        for (int i = firstBackslash; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                char next = text[i + 1];
                switch (next)
                {
                    case 'n':
                        sb.Append('\n');
                        i++;
                        continue;
                    case 'r':
                        sb.Append('\r');
                        i++;
                        continue;
                    case 't':
                        sb.Append('\t');
                        i++;
                        continue;
                    case '"':
                        sb.Append('"');
                        i++;
                        continue;
                    case '\'':
                        sb.Append('\'');
                        i++;
                        continue;
                    default:
                        sb.Append('\\');
                        continue;
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// A version of <see cref="Unescape"/> using Regex,
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