using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Tests.Library.TestUtils;

public static class GoldenNormalization
{
    public static string NormalizeMarkdown(string content)
    {
        if (content is null) return string.Empty;
        var s = NormalizeLineEndings(content);

        // Replace dynamic header values
        s = Regex.Replace(s, @"\*\*Generated:\*\* .* UTC", "**Generated:** <TS> UTC");
        s = Regex.Replace(s, @"\*\*Session ID:\*\* [a-f0-9]{8}", "**Session ID:** <ID8>");
        s = Regex.Replace(s, @"TestSession_[a-f0-9]{8}", "TestSession_<ID8>", RegexOptions.IgnoreCase);

        // Versions and runtime info
        s = Regex.Replace(s, @"Sailfish\s+[0-9]+\.[0-9]+\.[0-9]+(?:[^\s]*)?", "Sailfish <VER>", RegexOptions.IgnoreCase);
        // Normalize any .NET runtime version token like ".NET 8.0.18" or pre-release variants
        s = Regex.Replace(s, @"on \.NET\s+[^\r\n]+", "on .NET <VER>", RegexOptions.IgnoreCase);
        // Fallback: after Sailfish tokenized, collapse the remainder of the line to <VER>
        s = Regex.Replace(s, @"(Sailfish\s+<VER>\s+on\s+\.NET)\s+[^\r\n]+", "$1 <VER>", RegexOptions.IgnoreCase);

        // OS information (normalize across Windows, Linux, macOS)
        // Matches patterns like "- OS: Microsoft Windows 10.0.19045 (X64/X64)" or "- OS: Ubuntu 24.04.3 LTS (X64/X64)"
        // Use a simple line-by-line approach to ensure we catch the OS line
        var lines = s.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("- OS:"))
            {
                lines[i] = "- OS: <OS>";
            }
        }
        s = string.Join('\n', lines);

        // CI provider information (e.g., "- CI: GitHub Actions")
        s = Regex.Replace(s, @"- CI: .+\n?", "");

        // GUIDs anywhere
        s = Regex.Replace(s, @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}", "<GUID>", RegexOptions.IgnoreCase);

        // Timer calibration summaries
        s = Regex.Replace(s, @"High-resolution\s*\(~\s*\d+\s*ns\)", "High-resolution (~<NS> ns)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"freq=\d+\s*Hz,\s*res≈\s*\d+\s*ns,\s*baseline=\d+\s*ticks",
            "freq=<F> Hz, res≈<R> ns, baseline=<B> ticks", RegexOptions.IgnoreCase);

        // Trim trailing whitespace on each line
        s = Regex.Replace(s, @"[ \t]+(?=$)", string.Empty, RegexOptions.Multiline);

        // Remove empty lines that may have been left by CI provider removal
        s = Regex.Replace(s, @"\n\s*\n", "\n", RegexOptions.Multiline);

        return s.Trim();
    }

    public static string NormalizeCsv(string content)
    {
        if (content is null) return string.Empty;
        var s = NormalizeLineEndings(content);

        // Session metadata row: <id>,<timestampZ>,classes,tests
        s = Regex.Replace(s,
            pattern: @"^(?<id>[a-f0-9]{8}),(?<ts>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z),",
            replacement: "<ID8>,<TSZ>,",
            options: RegexOptions.Multiline | RegexOptions.IgnoreCase);

        // GUIDs anywhere
        s = Regex.Replace(s, @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}", "<GUID>", RegexOptions.IgnoreCase);

        // Trim trailing whitespace on each line
        s = Regex.Replace(s, @"[ \t]+(?=$)", string.Empty, RegexOptions.Multiline);

        return s.Trim();
    }

    public static string NormalizeLineEndings(string s)
    {
        return s.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Calculates the similarity percentage between two strings using line-by-line comparison.
    /// Returns a value between 0 and 100 representing the percentage of matching lines.
    /// </summary>
    /// <param name="actual">The actual string to compare</param>
    /// <param name="expected">The expected string to compare</param>
    /// <returns>Similarity percentage (0-100)</returns>
    public static double CalculateSimilarityPercentage(string actual, string expected)
    {
        if (actual == expected)
            return 100.0;

        var actualLines = actual.Split('\n');
        var expectedLines = expected.Split('\n');

        // Use Longest Common Subsequence approach for line-by-line comparison
        var lcsLength = ComputeLongestCommonSubsequence(actualLines, expectedLines);
        var maxLength = Math.Max(actualLines.Length, expectedLines.Length);

        if (maxLength == 0)
            return 100.0;

        return (lcsLength / (double)maxLength) * 100.0;
    }

    /// <summary>
    /// Computes the Longest Common Subsequence length between two arrays of strings.
    /// This is used to determine how many lines match between actual and expected output.
    /// </summary>
    private static int ComputeLongestCommonSubsequence(string[] actual, string[] expected)
    {
        var m = actual.Length;
        var n = expected.Length;

        // Create DP table
        var dp = new int[m + 1, n + 1];

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                if (actual[i - 1] == expected[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[m, n];
    }

    /// <summary>
    /// Generates a detailed diff report showing which lines differ between actual and expected strings.
    /// </summary>
    /// <param name="actual">The actual string</param>
    /// <param name="expected">The expected string</param>
    /// <param name="maxLinesToShow">Maximum number of differing lines to show in the report</param>
    /// <returns>A formatted diff report</returns>
    public static string GenerateDiffReport(string actual, string expected, int maxLinesToShow = 20)
    {
        var actualLines = actual.Split('\n');
        var expectedLines = expected.Split('\n');

        var sb = new StringBuilder();
        sb.AppendLine("=== DIFF REPORT ===");
        sb.AppendLine($"Actual lines: {actualLines.Length}, Expected lines: {expectedLines.Length}");
        sb.AppendLine();

        var diffCount = 0;
        var maxLine = Math.Max(actualLines.Length, expectedLines.Length);

        for (var i = 0; i < maxLine && diffCount < maxLinesToShow; i++)
        {
            var actualLine = i < actualLines.Length ? actualLines[i] : "<missing>";
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<missing>";

            if (actualLine != expectedLine)
            {
                sb.AppendLine($"Line {i + 1}:");
                sb.AppendLine($"  Expected: {TruncateForDisplay(expectedLine, 100)}");
                sb.AppendLine($"  Actual:   {TruncateForDisplay(actualLine, 100)}");
                sb.AppendLine();
                diffCount++;
            }
        }

        if (diffCount >= maxLinesToShow)
        {
            sb.AppendLine($"... and {Math.Max(0, CountDifferences(actualLines, expectedLines) - maxLinesToShow)} more differences");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Counts the total number of differing lines between two string arrays.
    /// </summary>
    private static int CountDifferences(string[] actual, string[] expected)
    {
        var count = 0;
        var maxLine = Math.Max(actual.Length, expected.Length);

        for (var i = 0; i < maxLine; i++)
        {
            var actualLine = i < actual.Length ? actual[i] : "<missing>";
            var expectedLine = i < expected.Length ? expected[i] : "<missing>";

            if (actualLine != expectedLine)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Truncates a string for display purposes, adding ellipsis if needed.
    /// </summary>
    private static string TruncateForDisplay(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
