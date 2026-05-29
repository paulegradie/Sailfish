using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Smoke tests for the HTML report renderer: structure, content, and graceful handling of missing data.
/// We assert on substrings rather than exact output to keep the tests robust against cosmetic CSS tweaks.
/// </summary>
public class ScaleFishHtmlReportTests
{
    [Fact]
    public void Build_EmptyResults_ProducesValidHtmlSkeleton()
    {
        var html = ScaleFishHtmlReportBuilder.Build(
            new List<ScalefishClassModel>(),
            new Dictionary<string, ComplexityMeasurement[]>());

        html.ShouldStartWith("<!doctype html>");
        html.ShouldContain("<title>ScaleFish Report</title>");
        html.ShouldContain("</html>");
        html.ShouldContain("ScaleFish complexity report");
    }

    [Fact]
    public void Build_WithModel_IncludesClassAndPropertyHeadings()
    {
        var (results, measurementsByKey) = BuildSingleEntryResults();

        var html = ScaleFishHtmlReportBuilder.Build(results, measurementsByKey);

        html.ShouldContain("Tests.Ns.Cls");
        html.ShouldContain("DoWork");
        html.ShouldContain("N");
        html.ShouldContain("Linear");
        html.ShouldContain("O(n)");
        html.ShouldContain("<svg"); // measurements present ⇒ plot rendered
    }

    [Fact]
    public void Build_WithoutMeasurements_OmitsPlotButKeepsTable()
    {
        var (results, _) = BuildSingleEntryResults();

        var html = ScaleFishHtmlReportBuilder.Build(results, new Dictionary<string, ComplexityMeasurement[]>());

        html.ShouldContain("Linear");
        html.ShouldNotContain("<svg");
        html.ShouldContain("Δ AICc"); // metrics table still rendered
    }

    [Fact]
    public void Build_Distinguishable_RendersGoodBadge()
    {
        var (results, measurementsByKey) = BuildSingleEntryResults(isDistinguishable: true);
        var html = ScaleFishHtmlReportBuilder.Build(results, measurementsByKey);
        html.ShouldContain("badge-good");
        html.ShouldNotContain("(uncertain)");
    }

    [Fact]
    public void Build_NotDistinguishable_RendersWarnBadge()
    {
        var (results, measurementsByKey) = BuildSingleEntryResults(isDistinguishable: false);
        var html = ScaleFishHtmlReportBuilder.Build(results, measurementsByKey);
        html.ShouldContain("badge-warn");
        html.ShouldContain("(uncertain)");
    }

    [Fact]
    public void Build_EscapesUnsafeContent()
    {
        var results = new List<ScalefishClassModel>
        {
            new("Ns.With<script>", "C&C", new List<ScaleFishMethodModel>
            {
                new("M\"1", new List<ScaleFishPropertyModel>
                {
                    new("N>0", BuildModel())
                })
            })
        };
        var html = ScaleFishHtmlReportBuilder.Build(results, new Dictionary<string, ComplexityMeasurement[]>());

        // Unsafe characters in identifiers should not be rendered raw.
        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("C&amp;C");
    }

    private static (List<ScalefishClassModel> results, Dictionary<string, ComplexityMeasurement[]> measurements)
        BuildSingleEntryResults(bool isDistinguishable = true)
    {
        var prop = new ScaleFishPropertyModel("Tests.Ns.Cls.DoWork.N", BuildModel(isDistinguishable));
        var method = new ScaleFishMethodModel("DoWork", new List<ScaleFishPropertyModel> { prop });
        var cls = new ScalefishClassModel("Tests.Ns", "Cls", new List<ScaleFishMethodModel> { method });

        var measurements = new[]
        {
            new ComplexityMeasurement(8, 8),
            new ComplexityMeasurement(16, 16),
            new ComplexityMeasurement(32, 32),
            new ComplexityMeasurement(64, 64),
            new ComplexityMeasurement(128, 128)
        };
        return (
            new List<ScalefishClassModel> { cls },
            new Dictionary<string, ComplexityMeasurement[]> { [prop.PropertyName] = measurements });
    }

    private static ScaleFishModel BuildModel(bool isDistinguishable = true)
    {
        var best = new Linear { FunctionParameters = new FittedCurve(scale: 1.0, bias: 0.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 0.5, bias: 0.0) };
        return new ScaleFishModel(
            scaleFishModelFunction: best,
            goodnessOfFit: 0.99,
            nextClosestScaleFishModelFunction: next,
            nextClosestGoodnessOfFit: 0.95,
            bestAicc: -50,
            nextBestAicc: -25,
            akaikeWeight: 0.999,
            isDistinguishable: isDistinguishable,
            sampleSize: 5,
            powerLog: new PowerLogResult(1.0, 1.0, 0.0, 0.0, 0.99));
    }
}
