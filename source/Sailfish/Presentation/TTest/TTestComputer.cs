using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;
using Sailfish.Utils;

namespace Sailfish.Presentation.TTest;

public class TTestComputer : ITTestComputer
{
    private readonly IFileIo fileIo;
    private readonly ITTest tTest;

    public TTestComputer(IFileIo fileIo, ITTest tTest)
    {
        this.fileIo = fileIo;
        this.tTest = tTest;
    }

    public List<NamedTTestResult> ComputeTTest(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings)
    {
        var before = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.BeforeFilePath);
        var after = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.AfterFilePath).ToList();

        var results = Compute(before, after, settings);
        return results;
    }

    private List<NamedTTestResult> Compute(List<TestCaseStatistics> before, List<TestCaseStatistics> after, TTestSettings testSettings)
    {
        var testNames = after.Select(x => x.DisplayName).OrderByDescending(x => x);

        var beforeData = before.ToList();
        var afterData = after.ToList();

        var results = new List<NamedTTestResult>();
        foreach (var testName in testNames)
        {
            var afterCompiled = afterData.Single(x => x.DisplayName == testName);
            var beforeCompiled = beforeData.SingleOrDefault(x => x.DisplayName == testName);

            if (beforeCompiled is not null) // this could be the first time we've ever run this test
            {
                var result = tTest.ExecuteTest(beforeCompiled.RawExecutionResults, afterCompiled.RawExecutionResults, testSettings);
                results.Add(new NamedTTestResult(testName, result));
            }
        }

        return results;
    }
}