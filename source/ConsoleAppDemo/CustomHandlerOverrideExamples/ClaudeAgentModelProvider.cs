using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.Ai;

namespace PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;

/// <summary>
///     Reference <see cref="ISailfishAgent" /> that drives the <c>claude</c> CLI as an agentic, code-aware
///     analyst. This is the "one seam, two power levels" payoff: the same interface that accepts a dumb one-shot
///     completion also accepts a full agent that <em>reads the code under test</em> and cites <c>file:line</c>.
///     <para>
///         It hands Skipper's grounded context to <c>claude -p</c> with read-only tools (Read/Grep/Glob) scoped to
///         the repository root, then parses the returned JSON into a <see cref="SkipperReview" />. Everything
///         degrades gracefully — if the CLI is absent or errors, a short note is returned and the benchmark run is
///         unaffected. Copy this into your own project and adapt the transport (Anthropic SDK, Bedrock, a local
///         model) to taste; the CLI flags below may need tweaking for your installed version.
///     </para>
/// </summary>
internal sealed class ClaudeAgentModelProvider : ISailfishAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SkipperReview> RunAsync(SkipperSession session, CancellationToken cancellationToken)
    {
        try
        {
            var raw = await InvokeClaudeAsync(BuildPrompt(session), session.RepositoryRoot, cancellationToken);
            return ParseReview(raw);
        }
        catch (Exception ex)
        {
            // The CLI may be missing, unauthenticated, or offline. Skipper is strictly additive — never throw.
            return SkipperReview.Empty with { ConsoleSummary = $"(Skipper unavailable: {ex.Message})" };
        }
    }

    private static string BuildPrompt(SkipperSession session)
    {
        var contextJson = JsonSerializer.Serialize(session.Context, JsonOptions);

        var sb = new StringBuilder();
        sb.AppendLine("You are Skipper, a performance analyst embedded in the Sailfish benchmarking library.");
        sb.AppendLine("A SailDiff comparison just completed. Explain WHAT changed and, by reading the code under test, WHY.");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Use ONLY the numbers in the context below. Never invent or recompute a measurement.");
        sb.AppendLine("- You MAY read files under the repository root to find the cause. Cite each as path:line.");
        sb.AppendLine("- If the environment shows concerns, or a change is not statistically significant, say so and temper the verdict.");
        sb.AppendLine("- Respond with ONLY a JSON object (no prose, no code fences) matching this schema:");
        sb.AppendLine("""
                      {
                        "overallVerdict": "Improved|Regressed|NotSignificant|Inconclusive",
                        "consoleSummary": "<=3 sentence plain-text summary",
                        "markdownReport": "a fuller markdown explanation that cites file:line",
                        "findings": [
                          {"testCaseDisplayName":"...","verdict":"Improved|Regressed|NotSignificant|Inconclusive","summary":"...","citedSourceLocations":["path:line"],"confidence":0.0}
                        ]
                      }
                      """);
        sb.AppendLine();
        sb.AppendLine("Context:");
        sb.AppendLine(contextJson);
        return sb.ToString();
    }

    private static async Task<string> InvokeClaudeAsync(string prompt, string repositoryRoot, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "claude",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Directory.Exists(repositoryRoot) ? repositoryRoot : Directory.GetCurrentDirectory()
        };
        startInfo.ArgumentList.Add("-p");                 // headless "print" mode; prompt arrives on stdin
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");
        startInfo.ArgumentList.Add("--allowedTools");     // read-only investigation only
        startInfo.ArgumentList.Add("Read");
        startInfo.ArgumentList.Add("Grep");
        startInfo.ArgumentList.Add("Glob");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.StandardInput.WriteAsync(prompt);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"claude exited with code {process.ExitCode}: {stderr}");

        return stdout;
    }

    private static SkipperReview ParseReview(string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
            return SkipperReview.Empty with { ConsoleSummary = raw.Trim(), MarkdownReport = raw.Trim() };

        try
        {
            var dto = JsonSerializer.Deserialize<ReviewDto>(json, JsonOptions);
            if (dto is null) return SkipperReview.Empty;

            var findings = (dto.Findings ?? new List<FindingDto>())
                .Select(f => new Finding(
                    f.TestCaseDisplayName ?? string.Empty,
                    f.Verdict,
                    f.Summary ?? string.Empty,
                    f.CitedSourceLocations ?? new List<string>(),
                    f.Confidence))
                .ToList();

            return new SkipperReview(
                dto.OverallVerdict,
                findings,
                Array.Empty<ProposedAction>(),
                dto.ConsoleSummary ?? string.Empty,
                dto.MarkdownReport ?? string.Empty);
        }
        catch
        {
            return SkipperReview.Empty with { ConsoleSummary = raw.Trim() };
        }
    }

    private static string? ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private sealed class ReviewDto
    {
        public SkipperVerdict OverallVerdict { get; set; }
        public string? ConsoleSummary { get; set; }
        public string? MarkdownReport { get; set; }
        public List<FindingDto>? Findings { get; set; }
    }

    private sealed class FindingDto
    {
        public string? TestCaseDisplayName { get; set; }
        public SkipperVerdict Verdict { get; set; }
        public string? Summary { get; set; }
        public List<string>? CitedSourceLocations { get; set; }
        public double Confidence { get; set; }
    }
}
