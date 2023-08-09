using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Distributions.Univariate;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Shouldly;
using Xunit;

namespace Test.Statistics;

public class ConvergenceChecks
{
    [Fact]
    public void TwoSampleWilcoxonSignedRankTestSailfish_NoChange()
    {
        var test = new TwoSampleWilcoxonSignedRankTestSailfish(new TestPreprocessor());

        var results = new List<TestResults>();
        for (var i = 0; i < 1000; i++)
        {
            var bf = GenerateRandomNormalDistribution(30, 10, 10);
            var af = GenerateRandomNormalDistribution(30, 11, 10);

            results.Add(test.ExecuteTest(bf, af, new TestSettings(0.0001, 4, false, TestType.TwoSampleWilcoxonSignedRankTest)));
        }

        var converged = results.All(x => x.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void TwoSampleWilcoxonSignedRankTestSailfish_Regresssed()
    {
        var test = new TwoSampleWilcoxonSignedRankTestSailfish(new TestPreprocessor());

        var results = new List<TestResults>();

        for (var i = 0; i < 1000; i++)
        {
            var bf = GenerateRandomNormalDistribution(30, 10, 1);
            var af = GenerateRandomNormalDistribution(30, 13, 1);

            results.Add(test.ExecuteTest(bf, af, new TestSettings(0.0001, 4, false, TestType.TwoSampleWilcoxonSignedRankTest)));
        }

        var converged = results.All(x => x.ChangeDescription == SailfishChangeDirection.Regressed);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_NoChange()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 5);
        var af = GenerateRandomNormalDistribution(30, 11, 5);

        var results = new List<TestResults>();

        var test = new MannWhitneyWilcoxonTestSailfish(new TestPreprocessor());
        for (var i = 0; i < 20; i++)
        {
            results.Add(test.ExecuteTest(bf, af, new TestSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));
        }

        var converged = results.All(x => x.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_Regressed()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 1);
        var af = GenerateRandomNormalDistribution(30, 15, 1);

        var results = new List<TestResults>();

        var test = new MannWhitneyWilcoxonTestSailfish(new TestPreprocessor());
        for (var i = 0; i < 20; i++)
        {
            results.Add(test.ExecuteTest(bf, af, new TestSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));
        }

        var converged = results.All(x => x.ChangeDescription == SailfishChangeDirection.Regressed);
        converged.ShouldBeTrue();
    }

    [Fact]
    public void MannWhitneyWilcoxonTestSailfish_HiStdDevNoChange()
    {
        var bf = GenerateRandomNormalDistribution(30, 10, 10);
        var af = GenerateRandomNormalDistribution(30, 15, 10);

        var results = new List<TestResults>();

        var test = new MannWhitneyWilcoxonTestSailfish(new TestPreprocessor());
        for (var i = 0; i < 20; i++)
        {
            results.Add(test.ExecuteTest(bf, af, new TestSettings(0.0001, 4, false, TestType.WilcoxonRankSumTest)));
        }

        var converged = results.All(x => x.ChangeDescription == SailfishChangeDirection.NoChange);
        converged.ShouldBeTrue();
    }

    private readonly Random random = new(42);

    double[] GenerateRandomNormalDistribution(int numSamples, double mean, double standardDeviation)
    {
        return new NormalDistribution(mean, standardDeviation).Generate(numSamples, random);
    }
}