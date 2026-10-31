using System.Buffers;
using System.Text;

namespace UniiaAnonim.TGBot.Shared.Extensions;

/// <summary>
/// Provides extension methods for escaping and formatting HTML safe for Telegram HTML parse mode.
/// </summary>
public static class TelegramHtmlExtensions
{
    private static readonly SearchValues<char> HtmlSpecialChars = SearchValues.Create(['&', '<', '>']);

    /// <summary>
    /// Escapes characters (&amp;, &lt;, &gt;) to ensure compatibility with Telegram HTML parse mode.
    /// Fast-path returns the original string if no special characters are present, resulting in zero allocations.
    /// </summary>
    /// <param name="text">The raw text to escape.</param>
    /// <returns>HTML-escaped string safe for Telegram parse mode.</returns>
    public static string EscapeTelegramHtml(this string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var span = text.AsSpan();
        int firstIndex = span.IndexOfAny(HtmlSpecialChars);
        if (firstIndex < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 16);
        sb.Append(span[..firstIndex]);
        span = span[firstIndex..];

        while (!span.IsEmpty)
        {
            int nextIndex = span.IndexOfAny(HtmlSpecialChars);
            if (nextIndex < 0)
            {
                sb.Append(span);
                break;
            }

            sb.Append(span[..nextIndex]);
            char ch = span[nextIndex];
            switch (ch)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }

            span = span[(nextIndex + 1)..];
        }

        return sb.ToString();
    }
}
