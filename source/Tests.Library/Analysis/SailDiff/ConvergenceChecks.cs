using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Contracts.Public;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class ConvergenceChecks
{
    private readonly Random random = new(42);

    [Fact]
    public void TwoSampleWilcoxonSignedRankTestSailfish_NoChange()
    {
        var test = new TwoSampleWilcoxonSignedRankTest(new TestPreprocessor(new SailfishOutlierDetector()));

        var results = new List<TestResultWithOutlierAnalysis>();
        for (var i = 0; i < 1000; i++)
        {
            var bf = GenerateRandomNormalDistribution(15, 10, 5);
            var af = GenerateRandomNormalDistribution(15, 11, 5);

            results.Add(test.ExecuteTest(bf, af, new SailDiffSettings(0.0001, 4, false)));
        }

        var converged = results.All(x => x.StatisticalTestResult.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void TwoSampleWilcoxonSignedRankTestSailfish_Regresssed()
    {
        var test = new TwoSampleWilcoxonSignedRankTest(new TestPreprocessor(new SailfishOutlierDetector()));

        var results = new List<TestResultWithOutlierAnalysis>();

        for (var i = 0; i < 1000; i++)
        {
            var bf = GenerateRandomNormalDistribution(30, 10, 1);
            var af = GenerateRandomNormalDistribution(30, 73, 1);
            results.Add(test.ExecuteTest(bf, af, new SailDiffSettings(0.0001, 4, false)));
        }

        var converged = results.All(x => x.StatisticalTestResult.ChangeDescription == SailfishChangeDirection.Regressed);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_NoChange()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 5);
        var af = GenerateRandomNormalDistribution(30, 11, 5);

        var results = new List<TestResultWithOutlierAnalysis>();

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        for (var i = 0; i < 20; i++) results.Add(test.ExecuteTest(bf, af, new SailDiffSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));

        var converged = results.All(x => x.StatisticalTestResult.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_Regressed()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 1);
        var af = GenerateRandomNormalDistribution(30, 20, 1);

        var results = new List<TestResultWithOutlierAnalysis>();

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        for (var i = 0; i < 20; i++) results.Add(test.ExecuteTest(bf, af, new SailDiffSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));

        var converged = results.All(x => x.StatisticalTestResult.ChangeDescription == SailfishChangeDirection.Regressed);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_HiStdDevNoChange()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 10);
        var af = GenerateRandomNormalDistribution(30, 20, 10);

        List<TestResultWithOutlierAnalysis> results = new();

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        for (var i = 0; i < 20; i++) results.Add(test.ExecuteTest(bf, af, new SailDiffSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));

        var converged = results.All(x => x.StatisticalTestResult.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    private double[] GenerateRandomNormalDistribution(int sampleSize, double mean, double standardDeviation)
    {
        return TestDistributions.GenerateRandomNormalDistribution(sampleSize, mean, standardDeviation, random);
    }
}