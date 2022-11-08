﻿using Sailfish.Exceptions;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTest;
using Sailfish.Statistics.Tests.TTest;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTest;

namespace Sailfish.Analysis;

public class StatisticalTestExecutor : IStatisticalTestExecutor
{
    private readonly IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTest;
    private readonly ITTest ttest;
    private readonly ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTest;

    public StatisticalTestExecutor(
        IMannWhitneyWilcoxonTest? mwWilcoxonTest = null,
        ITTest? ttest = null,
        ITwoSampleWilcoxonSignedRankTest? twoSampWilcoxonSignedRankTest = null)
    {
        this.mannWhitneyWilcoxonTest = mwWilcoxonTest ?? new MannWhitneyWilcoxonTest();
        this.ttest = ttest ?? new TTest();
        this.twoSampWilcoxonSignedRankTest = twoSampWilcoxonSignedRankTest ?? new TwoSampleWilcoxonSignedRankTest();
    }

    public TestResults ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        TestSettings settings)
    {
        return settings.TestType switch
        {
            TestType.TTest => ttest.ExecuteTest(beforeData, afterData, settings),
            TestType.WilcoxonRankSumTest => mannWhitneyWilcoxonTest.ExecuteTest(beforeData, afterData, settings),
            TestType.TwoSampleWilcoxonSignedRankTest => twoSampWilcoxonSignedRankTest.ExecuteTest(beforeData, afterData, settings),
            _ => throw new SailfishException($"Test type {settings.TestType.ToString()} not supported")
        };
    }
}