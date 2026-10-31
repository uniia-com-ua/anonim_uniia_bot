using System.Text.RegularExpressions;

namespace UniiaAnonim.TGBot.Shared.Consts;

/// <summary>
/// Provides source-generated, high-performance regular expressions shared across the application.
/// </summary>
public static partial class CommonRegexes
{
    /// <summary>
    /// Matches a standard 36-character hyphenated GUID (UUID).
    /// </summary>
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    public static partial Regex GuidRegex();
}
