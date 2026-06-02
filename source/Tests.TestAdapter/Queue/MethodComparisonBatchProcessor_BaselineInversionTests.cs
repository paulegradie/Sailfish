using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Regression coverage for the inverted-baseline reporting bug in the IDE method-comparison
/// output (surfaced on Sailfish.TestAdapter 4.0.136).
///
/// <para>
/// Ground truth fed in synthetically (NO real timing): the method flagged
/// <c>IsBaseline = true</c> (<c>WithPlus</c>) is the SLOWEST at every variable value, and the
/// candidate (<c>WithStringBuilder</c>) is the FASTEST. WithPlus's smallest mean (8 ms) is still
/// larger than WithStringBuilder's largest mean (4 ms), so for any pairing of the two methods —
/// even across different N — WithPlus is unambiguously slower.
/// </para>
///
/// <para>
/// A correct comparison report must therefore say WithStringBuilder is FASTER than the WithPlus
/// baseline, and must name WithPlus (the only <c>IsBaseline</c> method) as the baseline. These
/// tests drive the real <see cref="SailDiffUnifiedFormatter"/> / <see cref="ImpactSummaryFormatter"/>
/// through the production <see cref="MethodComparisonBatchProcessor"/> and assert exactly that.
/// They FAIL against current code, demonstrating the bug.
/// </para>
/// </summary>
public class MethodComparisonBatchProcessorBaselineInversionTests
{
    private const string ClassName = "SailfishBugRepro.BaselineVerdictInversionRepro";
    private const string Group = "Concat";

    // The single IsBaseline = true method, and its (deliberately slow) per-case means in ms.
    private const string BaselineMethod = "WithPlus";

    // name (as printed by ExtractMethodName) -> true mean in ms.
    private static readonly Dictionary<string, double> TrueMeanMs = new(StringComparer.Ordinal)
    {
        ["WithPlus(N: 100)"] = 8.0,     // baseline, slow
        ["WithPlus(N: 10000)"] = 240.0, // baseline, slow
        ["WithStringBuilder(N: 100)"] = 1.0,    // candidate, fast
        ["WithStringBuilder(N: 10000)"] = 4.0,  // candidate, fast
    };

    private readonly ITestOutputHelper _output;
    private readonly IAdapterSailDiff _sailDiff = Substitute.For<IAdapterSailDiff>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    // The REAL unified formatter — the bug lives in the formatting/operand path, so a mocked
    // formatter (as in the sibling Accumulate tests) would hide it.
    private readonly ISailDiffUnifiedFormatter _formatter = SailDiffUnifiedFormatterFactory.Create();

    public MethodComparisonBatchProcessorBaselineInversionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task IdeComparisonOutput_DoesNotInvertOrMislabelTheBaseline()
    {
        // Arrange: a faithful fake of AdapterSailDiff. It maps operands the same way the real one
        // does (before = preloaded run, after = the summary result under the common id), so the
        // before/after means are exactly what the production code would feed the formatter.
        _sailDiff.ComputeTestCaseDiff(default!, default!, default!, default!, default!)
            .ReturnsForAnyArgs(ci =>
            {
                var currentId = ci.ArgAt<string>(2);
                var summary = ci.ArgAt<IClassExecutionSummary>(3);
                var preloadedBefore = ci.ArgAt<PerformanceRunResult>(4);

                var beforeMean = preloadedBefore.Mean;
                var afterMean = summary.CompiledTestCaseResults
                    .First(x => x.PerformanceRunResult is not null
                                && x.PerformanceRunResult.DisplayName == currentId)
                    .PerformanceRunResult!.Mean;

                return BuildDiff(beforeMean, afterMean);
            });

        var published = new List<FrameworkTestCaseEndNotification>();
        _mediator
            .When(m => m.Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>()))
            .Do(ci => published.Add(ci.Arg<FrameworkTestCaseEndNotification>()));

        // Two methods across two SailfishVariable values => 4 cases. Baseline (WithPlus) = large mean.
        var p100 = CreateMessage("WithPlus(N: 100)", TrueMeanMs["WithPlus(N: 100)"], isBaseline: true);
        var p10000 = CreateMessage("WithPlus(N: 10000)", TrueMeanMs["WithPlus(N: 10000)"], isBaseline: true);
        var sb100 = CreateMessage("WithStringBuilder(N: 100)", TrueMeanMs["WithStringBuilder(N: 100)"], isBaseline: false);
        var sb10000 = CreateMessage("WithStringBuilder(N: 10000)", TrueMeanMs["WithStringBuilder(N: 10000)"], isBaseline: false);

        var all = new[] { p100, p10000, sb100, sb10000 };
        AttachClassExecutionSummary(all);

        var batch = new TestCaseBatch
        {
            BatchId = $"Comparison_{ClassName}_{Group}",
            TestCases = all.ToList(),
            Status = BatchStatus.Complete,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        };

        var sut = new MethodComparisonBatchProcessor(_sailDiff, _mediator, _logger, _formatter);

        // Act
        await sut.ProcessBatch(batch, CancellationToken.None);

        // Collect every IMPACT verdict line the user would see, from every method's row.
        var verdicts = published
            .SelectMany(n => ParseVerdicts(n.TestOutputWindowMessage))
            .ToList();

        foreach (var n in published)
        {
            _output.WriteLine($"----- {n.TestCase.FullyQualifiedName} -----");
            _output.WriteLine(n.TestOutputWindowMessage);
        }

        verdicts.ShouldNotBeEmpty("expected the IDE comparison output to contain '… IMPACT: …' verdict lines");

        var failures = new List<string>();

        foreach (var v in verdicts)
        {
            var comparedName = StripIcon(v.Compared);
            var primaryName = v.Primary;

            // (c) — no row may compare a case against itself.
            if (string.Equals(comparedName, primaryName, StringComparison.Ordinal))
                failures.Add($"SELF-PAIR: '{v.Line}'");

            var haveCompared = TrueMeanMs.TryGetValue(comparedName, out var comparedTrue);
            var havePrimary = TrueMeanMs.TryGetValue(primaryName, out var primaryTrue);
            if (!haveCompared || !havePrimary)
            {
                failures.Add($"UNRECOGNIZED METHOD NAME in: '{v.Line}'");
                continue;
            }

            // (b) — direction must match ground truth: "faster" iff the compared method's true mean
            // is below the named baseline's true mean.
            var expectedFaster = comparedTrue < primaryTrue;
            var reportedFaster = v.Direction == "faster";
            if (reportedFaster != expectedFaster)
                failures.Add(
                    $"INVERTED VERDICT: '{v.Line}' — reported '{v.Direction}', but {comparedName} (mean {comparedTrue}ms) " +
                    $"vs baseline {primaryName} (mean {primaryTrue}ms) should be '{(expectedFaster ? "faster" : "slower")}'");

            // (c) — printed means must not be transposed: the mean printed beside the named baseline
            // must order the same way as that baseline's true mean relative to the compared method.
            var printedPrimaryAbove = v.PrimaryMean > v.ComparedMean;
            var truePrimaryAbove = primaryTrue > comparedTrue;
            if (printedPrimaryAbove != truePrimaryAbove)
                failures.Add(
                    $"TRANSPOSED MEANS: '{v.Line}' — printed baseline mean {v.PrimaryMean} → compared {v.ComparedMean}, " +
                    $"but baseline {primaryName} (true {primaryTrue}ms) vs {comparedName} (true {comparedTrue}ms) are the other way round");

            // (a) — when the two methods differ, the baseline named must be the IsBaseline method.
            var comparedIsBaseline = comparedName.StartsWith(BaselineMethod, StringComparison.Ordinal);
            var primaryIsBaseline = primaryName.StartsWith(BaselineMethod, StringComparison.Ordinal);
            var mixedPair = comparedIsBaseline ^ primaryIsBaseline;
            if (mixedPair && !primaryIsBaseline)
                failures.Add(
                    $"WRONG BASELINE IDENTITY: '{v.Line}' — '{primaryName}' is labelled the baseline, " +
                    $"but the only IsBaseline=true method is '{BaselineMethod}'");
        }

        // Crisp restatement of the exact reported symptom: no line may claim a WithPlus case is
        // FASTER than a WithStringBuilder baseline (WithPlus is always the slower method here).
        var inverted = verdicts.FirstOrDefault(v =>
            StripIcon(v.Compared).StartsWith("WithPlus", StringComparison.Ordinal) &&
            v.Primary.StartsWith("WithStringBuilder", StringComparison.Ordinal) &&
            v.Direction == "faster");
        if (inverted is not null)
            failures.Add($"REPORTED SYMPTOM REPRODUCED: '{inverted.Line}'");

        failures.ShouldBeEmpty(
            "Baseline comparison verdicts are inverted/mislabelled:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Locks the post-fix formatter contract: <see cref="ImpactSummaryFormatter"/> renders comparison
    /// data verbatim — the named baseline (<c>PrimaryMethodName</c>) always carries MeanBefore and the
    /// contender (<c>ComparedMethodName</c>) MeanAfter. The producer is now responsible for orienting
    /// the statistics, so the formatter must NOT re-derive or swap anything (the historical
    /// perspective heuristic is gone). A regression here would resurrect the inverted verdict.
    /// </summary>
    [Fact]
    public void ImpactSummary_RendersPreOrientedComparison_WithoutTransposing()
    {
        const double baselineMeanMs = 200.0;   // WithPlus — slow baseline
        const double contenderMeanMs = 2.0;    // WithStringBuilder — fast contender

        // Pre-oriented by the producer: Before = baseline (WithPlus), After = contender (StringBuilder).
        var stats = BuildStat(meanBefore: baselineMeanMs, meanAfter: contenderMeanMs);

        var data = new SailDiffComparisonData
        {
            GroupName = Group,
            PrimaryMethodName = "WithPlus(N: 100)",           // the baseline-flagged method
            ComparedMethodName = "WithStringBuilder(N: 100)", // the contender
            IsPerspectiveBased = false,
            Statistics = stats,
            Metadata = new ComparisonMetadata { AlphaLevel = 0.05 }
        };

        var summary = new ImpactSummaryFormatter().CreateImpactSummary(data, OutputContext.Ide);
        _output.WriteLine(summary);

        var verdict = ParseVerdicts(summary).Single();

        verdict.Primary.ShouldBe("WithPlus(N: 100)", $"the baseline-flagged method must be named the baseline. Got: '{verdict.Line}'");
        StripIcon(verdict.Compared).ShouldBe("WithStringBuilder(N: 100)");
        verdict.Direction.ShouldBe("faster",
            $"the contender (~{contenderMeanMs}ms) is faster than the baseline (~{baselineMeanMs}ms). Got: '{verdict.Line}'");
        verdict.PrimaryMean.ShouldBeGreaterThan(verdict.ComparedMean,
            $"the baseline (WithPlus, ~{baselineMeanMs}ms) must print a LARGER mean than the contender " +
            $"(WithStringBuilder, ~{contenderMeanMs}ms); a smaller baseline mean would mean the name/mean pair was transposed. Got: '{verdict.Line}'");
    }

    // ---------- helpers ----------

    private sealed record Verdict(string Line, string Compared, double Pct, string Direction, string Primary, double PrimaryMean, double ComparedMean);

    private static readonly Regex VerdictRx = new(
        @"IMPACT:\s*(?<compared>.+?)\s+is\s+(?<pct>[\d.]+)%\s+(?<dir>faster|slower)\s+than\s+baseline\s+(?<primary>.+?)\s+\(",
        RegexOptions.Compiled);

    private static readonly Regex MeanRx = new(
        @"Mean:\s*(?<pt>[\d.]+)\s*\S+\s*→\s*(?<ct>[\d.]+)",
        RegexOptions.Compiled);

    private static IEnumerable<Verdict> ParseVerdicts(string output)
    {
        var lines = output.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var vm = VerdictRx.Match(lines[i]);
            if (!vm.Success) continue;

            double pt = double.NaN, ct = double.NaN;
            // The "P-Value … | Mean: A → B" detail is the next non-empty line.
            for (var j = i + 1; j < Math.Min(lines.Length, i + 3); j++)
            {
                var mm = MeanRx.Match(lines[j]);
                if (mm.Success)
                {
                    pt = double.Parse(mm.Groups["pt"].Value, CultureInfo.InvariantCulture);
                    ct = double.Parse(mm.Groups["ct"].Value, CultureInfo.InvariantCulture);
                    break;
                }
            }

            yield return new Verdict(
                lines[i].Trim(),
                vm.Groups["compared"].Value.Trim(),
                double.Parse(vm.Groups["pct"].Value, CultureInfo.InvariantCulture),
                vm.Groups["dir"].Value,
                vm.Groups["primary"].Value.Trim(),
                pt,
                ct);
        }
    }

    // The compared token includes the leading significance icon (🟢/🔴/⚪); strip it for name compares.
    private static string StripIcon(string compared)
    {
        var idx = compared.IndexOf("IMPACT:", StringComparison.Ordinal);
        var s = idx >= 0 ? compared[(idx + "IMPACT:".Length)..] : compared;
        return s.Trim();
    }

    private TestCompletionQueueMessage CreateMessage(string methodWithVariables, double meanMs, bool isBaseline)
    {
        var fqn = $"{ClassName}.{methodWithVariables}";
        var testCase = new TestCase(fqn, new Uri("executor://sailfish"), "Sailfish");
        return new TestCompletionQueueMessage
        {
            TestCaseId = fqn,
            TestResult = new TestExecutionResult { IsSuccess = true },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["ComparisonGroup"] = Group,
                ["ComparisonRole"] = isBaseline ? "Baseline" : "Comparison",
                ["TestCase"] = testCase,
                ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1),
                ["EndTime"] = DateTimeOffset.UtcNow,
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = meanMs,
                MedianMs = meanMs,
                SampleSize = 5,
                StandardDeviation = meanMs * 0.01,
                Variance = 1.0,
                RawExecutionResults = new[] { meanMs, meanMs, meanMs },
                DataWithOutliersRemoved = new[] { meanMs, meanMs, meanMs },
                UpperOutliers = Array.Empty<double>(),
                LowerOutliers = Array.Empty<double>(),
                TotalNumOutliers = 0,
                GroupingId = Group,
                NumWarmupIterations = 0
            }
        };
    }

    private static StatisticalTestResult BuildStat(double meanBefore, double meanAfter)
    {
        // Raw arrays must carry the means: ImpactSummaryFormatter recomputes display means from them.
        var rawBefore = new[] { meanBefore, meanBefore, meanBefore };
        var rawAfter = new[] { meanAfter, meanAfter, meanAfter };
        return new StatisticalTestResult(
            meanBefore: meanBefore,
            meanAfter: meanAfter,
            medianBefore: meanBefore,
            medianAfter: meanAfter,
            testStatistic: 5.0,
            pValue: 0.0000001,                 // strongly significant
            changeDescription: "Significant",  // must not contain "No Change"
            sampleSizeBefore: rawBefore.Length,
            sampleSizeAfter: rawAfter.Length,
            rawDataBefore: rawBefore,
            rawDataAfter: rawAfter,
            additionalResults: new Dictionary<string, object>());
    }

    private static TestCaseSailDiffResult BuildDiff(double meanBefore, double meanAfter)
    {
        var stat = BuildStat(meanBefore, meanAfter);
        var withOutliers = new TestResultWithOutlierAnalysis(
            stat,
            new ProcessedStatisticalTestData(stat.RawDataBefore, stat.RawDataBefore, Array.Empty<double>(), Array.Empty<double>(), 0),
            new ProcessedStatisticalTestData(stat.RawDataAfter, stat.RawDataAfter, Array.Empty<double>(), Array.Empty<double>(), 0));
        var diff = new SailDiffResult(new TestCaseId("Comparison"), withOutliers);
        return new TestCaseSailDiffResult(
            new List<SailDiffResult> { diff },
            new TestIds(new[] { "before" }, new[] { "after" }),
            new Sailfish.Analysis.SailDiff.SailDiffSettings());
    }

    private static void AttachClassExecutionSummary(params TestCompletionQueueMessage[] messages)
    {
        var compiled = new List<ICompiledTestCaseResult>();
        foreach (var m in messages)
        {
            var pm = m.PerformanceMetrics;
            var clean = pm.DataWithOutliersRemoved ?? pm.RawExecutionResults ?? Array.Empty<double>();
            var n = clean.Length;
            var se = n > 1 ? pm.StandardDeviation / Math.Sqrt(n) : 0;
            var pr = new PerformanceRunResult(
                m.TestCaseId,
                pm.MeanMs,
                pm.StandardDeviation,
                pm.Variance,
                pm.MedianMs,
                pm.RawExecutionResults ?? Array.Empty<double>(),
                pm.SampleSize,
                pm.NumWarmupIterations,
                clean,
                pm.UpperOutliers ?? Array.Empty<double>(),
                pm.LowerOutliers ?? Array.Empty<double>(),
                pm.TotalNumOutliers,
                se,
                0.95,
                pm.MeanMs,
                pm.MeanMs,
                0.0);
            compiled.Add(new StubCompiledResult(new TestCaseId(m.TestCaseId), pm.GroupingId ?? string.Empty, pr));
        }

        var summary = new StubClassExecutionSummary(typeof(object), new ExecutionSettings(), compiled);
        foreach (var m in messages)
        {
            m.Metadata["ClassExecutionSummaries"] = summary;
        }
    }

    private sealed class StubCompiledResult : ICompiledTestCaseResult
    {
        public StubCompiledResult(TestCaseId id, string grouping, PerformanceRunResult pr)
        {
            TestCaseId = id;
            GroupingId = grouping;
            PerformanceRunResult = pr;
        }

        public string? GroupingId { get; }
        public PerformanceRunResult? PerformanceRunResult { get; }
        public Exception? Exception { get; } = null;
        public TestCaseId? TestCaseId { get; }
    }

    private sealed class StubClassExecutionSummary : IClassExecutionSummary
    {
        public StubClassExecutionSummary(Type type, IExecutionSettings settings, IEnumerable<ICompiledTestCaseResult> results)
        {
            TestClass = type;
            ExecutionSettings = settings;
            CompiledTestCaseResults = results;
        }

        public Type TestClass { get; }
        public IExecutionSettings ExecutionSettings { get; }
        public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }
        public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases() => CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
        public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases() => CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
        public IClassExecutionSummary FilterForSuccessfulTestCases() => new StubClassExecutionSummary(TestClass, ExecutionSettings, GetSuccessfulTestCases());
        public IClassExecutionSummary FilterForFailureTestCases() => new StubClassExecutionSummary(TestClass, ExecutionSettings, GetFailedTestCases());
    }
}
