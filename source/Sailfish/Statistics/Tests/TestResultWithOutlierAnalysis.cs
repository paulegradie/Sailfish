using System;
using System.Collections.Generic;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics.Tests;

public class TestResultWithOutlierAnalysis
{
    public TestResultWithOutlierAnalysis(TestResults testResults, OutlierAnalysis? sample1, OutlierAnalysis? sample2)
    {
        TestResults = testResults;
        Sample1 = sample1;
        Sample2 = sample2;
        ExceptionMessage = string.Empty;
        StackTrace = string.Empty;
    }

    public TestResultWithOutlierAnalysis(Exception exception)
    {
        TestResults = new TestResults(
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            float.NaN,
            string.Empty,
            0,
            0,
            new double[] { },
            new double[] { },
            new Dictionary<string, object>());
        ExceptionMessage = exception.Message;
        StackTrace = exception.StackTrace ?? string.Empty;
    }

    public string ExceptionMessage { get; init; }
    public string StackTrace { get; init; }
    public TestResults TestResults { get; init; }
    public OutlierAnalysis? Sample1 { get; init; }
    public OutlierAnalysis? Sample2 { get; init; }
}