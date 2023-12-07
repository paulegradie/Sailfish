using Sailfish.Contracts.Public.Models;
using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests;

public class TestResultWithOutlierAnalysis
{
    public TestResultWithOutlierAnalysis(StatisticalTestResult statisticalTestResult, ProcessedStatisticalTestData? sample1, ProcessedStatisticalTestData? sample2)
    {
        StatisticalTestResult = statisticalTestResult;
        Sample1 = sample1;
        Sample2 = sample2;
        ExceptionMessage = string.Empty;
        StackTrace = string.Empty;
    }

    public TestResultWithOutlierAnalysis(Exception exception)
    {
        StatisticalTestResult = new StatisticalTestResult(
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            string.Empty,
            0,
            0,
            [],
            [],
            new Dictionary<string, object>());
        ExceptionMessage = exception.Message;
        StackTrace = exception.StackTrace ?? string.Empty;
    }

    public string ExceptionMessage { get; init; }
    public string StackTrace { get; init; }
    public StatisticalTestResult StatisticalTestResult { get; init; }
    public ProcessedStatisticalTestData? Sample1 { get; init; }
    public ProcessedStatisticalTestData? Sample2 { get; init; }
}