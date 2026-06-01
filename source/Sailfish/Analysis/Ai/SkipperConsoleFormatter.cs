using System.Text;

namespace Sailfish.Analysis.Ai;

internal interface ISkipperConsoleFormatter
{
    string Format(SkipperReview review);
}

/// <summary>
///     Renders a <see cref="SkipperReview" /> into the inline console block shown beneath the SailDiff table.
///     Owning the formatting here (rather than trusting the agent's prose) keeps the verdict vocabulary and the
///     🔴/🟢/⚪/🟡 chips consistent no matter which model produced the review.
/// </summary>
internal sealed class SkipperConsoleFormatter : ISkipperConsoleFormatter
{
    public string Format(SkipperReview review)
    {
        var sb = new StringBuilder();
        sb.Append("🧭 Skipper  ").Append(Chip(review.OverallVerdict)).Append(' ').Append(Label(review.OverallVerdict));

        if (!string.IsNullOrWhiteSpace(review.ConsoleSummary))
        {
            sb.AppendLine();
            sb.Append(review.ConsoleSummary.Trim());
        }

        foreach (var finding in review.Findings)
        {
            sb.AppendLine();
            sb.Append("  • ").Append(Chip(finding.Verdict)).Append(' ')
                .Append(finding.TestCaseDisplayName).Append(" — ").Append(finding.Summary.Trim());

            if (finding.CitedSourceLocations is { Count: > 0 })
            {
                sb.AppendLine();
                sb.Append("       ↳ ").Append(string.Join(", ", finding.CitedSourceLocations));
            }
        }

        return sb.ToString();
    }

    private static string Chip(SkipperVerdict verdict) => verdict switch
    {
        SkipperVerdict.Regressed => "🔴",
        SkipperVerdict.Improved => "🟢",
        SkipperVerdict.NotSignificant => "⚪",
        _ => "🟡"
    };

    private static string Label(SkipperVerdict verdict) => verdict switch
    {
        SkipperVerdict.Regressed => "REGRESSED",
        SkipperVerdict.Improved => "IMPROVED",
        SkipperVerdict.NotSignificant => "NOT SIGNIFICANT",
        _ => "INCONCLUSIVE"
    };
}
