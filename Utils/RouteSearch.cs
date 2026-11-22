using System.Globalization;
using System.Text;

namespace IvanConnections_Travel.Utils;

/// <summary>
/// Lenient route-name matching: ignores case, diacritics and separators, so "cu" finds "C-U"
/// and "1 b" finds "1B".
/// </summary>
public static class RouteSearch
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToUpperInvariant(c));
        }
        return builder.ToString();
    }

    public static bool AreEquivalent(string? a, string? b)
    {
        var na = Normalize(a);
        return na.Length > 0 && na == Normalize(b);
    }

    /// <summary>True if <paramref name="candidate"/> contains <paramref name="query"/> (lenient).</summary>
    public static bool Matches(string? candidate, string? query)
    {
        var q = Normalize(query);
        return q.Length == 0 || Normalize(candidate).Contains(q, StringComparison.Ordinal);
    }

    public static bool StartsWith(string? candidate, string? query)
    {
        var q = Normalize(query);
        return q.Length == 0 || Normalize(candidate).StartsWith(q, StringComparison.Ordinal);
    }
}
