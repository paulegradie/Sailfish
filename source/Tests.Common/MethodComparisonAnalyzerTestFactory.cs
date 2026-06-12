using Sailfish;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

namespace Tests.Common;

/// <summary>
/// Builds a real <see cref="MethodComparisonAnalyzer" /> wired to the production statistical engine, for
/// tests that exercise method-comparison output end-to-end (so the verdict matches what ships).
/// </summary>
public static class MethodComparisonAnalyzerTestFactory
{
    public static IMethodComparisonAnalyzer Create()
    {
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());
        var executor = new StatisticalTestExecutor(
            new MannWhitneyWilcoxonTest(preprocessor),
            new Test(preprocessor),
            new TwoSampleWilcoxonSignedRankTest(preprocessor),
            new KolmogorovSmirnovTest(preprocessor),
            new PermutationTest(preprocessor));
        var computer = new StatisticalTestComputer(executor, new PerformanceRunResultAggregator());
        return new MethodComparisonAnalyzer(computer);
    }

    /// <summary>A minimal real <see cref="IRunSettings" /> with SailDiff enabled, for wiring the IDE batch processor.</summary>
    public static IRunSettings CreateRunSettings()
    {
        return RunSettingsBuilder.CreateBuilder().WithSailDiff().Build();
    }
}
