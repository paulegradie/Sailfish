using System;
using Sailfish.Analysis.Ai;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class SkipperConsoleFormatterTests
{
    private readonly SkipperConsoleFormatter formatter = new();

    [Fact]
    public void Format_RendersOverallVerdictChipAndLabel()
    {
        var review = new SkipperReview(
            SkipperVerdict.Regressed,
            Array.Empty<Finding>(),
            Array.Empty<ProposedAction>(),
            "ParseHeaders got slower.",
            string.Empty);

        var output = formatter.Format(review);

        output.ShouldContain("🧭 Skipper");
        output.ShouldContain("🔴");
        output.ShouldContain("REGRESSED");
        output.ShouldContain("ParseHeaders got slower.");
    }

    [Fact]
    public void Format_RendersFindingsWithChipsAndCitations()
    {
        var review = new SkipperReview(
            SkipperVerdict.Regressed,
            new[]
            {
                new Finding("Bench.ParseHeaders", SkipperVerdict.Regressed, "regex allocated in loop",
                    new[] { "Parser.cs:88" }, 0.9)
            },
            Array.Empty<ProposedAction>(),
            string.Empty,
            string.Empty);

        var output = formatter.Format(review);

        output.ShouldContain("Bench.ParseHeaders");
        output.ShouldContain("regex allocated in loop");
        output.ShouldContain("Parser.cs:88");
    }

    [Theory]
    [InlineData(SkipperVerdict.Improved, "🟢", "IMPROVED")]
    [InlineData(SkipperVerdict.NotSignificant, "⚪", "NOT SIGNIFICANT")]
    [InlineData(SkipperVerdict.Inconclusive, "🟡", "INCONCLUSIVE")]
    public void Format_MapsVerdictToChipAndLabel(SkipperVerdict verdict, string chip, string label)
    {
        var review = new SkipperReview(verdict, Array.Empty<Finding>(), Array.Empty<ProposedAction>(), "narrative", string.Empty);

        var output = formatter.Format(review);

        output.ShouldContain(chip);
        output.ShouldContain(label);
    }
}
