using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The framework's default <see cref="ISkipperResponseParser" />. Twin of the output-schema contract emitted by
///     <see cref="DefaultSkipperPromptBuilder" />: the DTO field names below ARE that schema. Change the two together;
///     <c>SkipperOutputContractTests</c> locks them in sync.
///     <para>
///         Tolerant by design — it pulls the first JSON object out of the reply (models occasionally wrap it in
///         prose despite instructions), and any malformed payload degrades to a review carrying the raw text rather
///         than throwing.
///     </para>
/// </summary>
internal sealed class DefaultSkipperResponseParser : ISkipperResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SkipperReview Parse(string modelText)
    {
        if (string.IsNullOrWhiteSpace(modelText))
        {
            return SkipperReview.Empty;
        }

        var json = ExtractJsonObject(modelText);
        if (json is null)
        {
            var raw = modelText.Trim();
            return SkipperReview.Empty with { ConsoleSummary = raw, MarkdownReport = raw };
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ReviewDto>(json, JsonOptions);
            if (dto is null)
            {
                return SkipperReview.Empty;
            }

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
            return SkipperReview.Empty with { ConsoleSummary = modelText.Trim() };
        }
    }

    /// <summary>Extract the outermost <c>{ ... }</c> span, tolerating leading / trailing prose or code fences.</summary>
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
