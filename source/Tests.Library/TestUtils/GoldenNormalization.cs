using System;
using System.Globalization;
using System.IO;
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
        for (int i = 0; i < lines.Length; i++)
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
}
