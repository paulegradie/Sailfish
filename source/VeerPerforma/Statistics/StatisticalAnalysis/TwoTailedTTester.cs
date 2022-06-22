using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeerPerforma.Presentation.Console;
using VeerPerforma.Presentation.Csv;
using VeerPerforma.Utils;

namespace VeerPerforma.Statistics.StatisticalAnalysis;

public class TwoTailedTTester : ITwoTailedTTester
{
    private readonly ITrackingFileFinder trackingFileFinder;
    private readonly IFileIo fileIo;
    private readonly ITTest ttest;
    private readonly IPresentationStringConstructor stringBuilder;

    public TwoTailedTTester(
        ITrackingFileFinder trackingFileFinder,
        IFileIo fileIo,
        ITTest ttest,
        IPresentationStringConstructor stringBuilder)
    {
        this.trackingFileFinder = trackingFileFinder;
        this.fileIo = fileIo;
        this.ttest = ttest;
        this.stringBuilder = stringBuilder;
    }

    public async Task PresentTestResults(string readDirectory, string outputFilePath)
    {
        await Task.CompletedTask;
        var beforeAndAfter = trackingFileFinder.GetBeforeAndAfterTrackingFiles(readDirectory);
        if (string.IsNullOrEmpty(beforeAndAfter.Before)) return;
        if (string.IsNullOrEmpty(beforeAndAfter.After)) return;

        var results = ComputeTTest(beforeAndAfter);

        var table = results.ToStringTable(
            new List<string>() { "", "ms", "ms", "", "", "", ""},
            m => m.TestName,
            m => m.MeanOfBefore,
            m => m.MeanOfAfter,
            m => m.PValue,
            m => m.DegreesOfFreedom,
            m => m.TStatistic,
            m => m.ChangeDescription
        );
        PrintHeader(Path.GetFileName(beforeAndAfter.Before), Path.GetFileName(beforeAndAfter.After));
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(table);

        var fileString = stringBuilder.Build();
        if (!string.IsNullOrEmpty(fileString))
            await fileIo.WriteToFile(fileString, outputFilePath, CancellationToken.None);
    }

    private void PrintHeader(string beforeId, string afterId)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("\r-----------------------------------");
        stringBuilder.AppendLine($"\r - T-Test results comparing - \rBefore: {beforeId}\rAfter: {afterId} \r");
        stringBuilder.AppendLine("-----------------------------------\r");
    }

    private List<NamedTTestResult> ComputeTTest(BeforeAndAfterTrackingFiles beforeAndAfter)
    {
        var before = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.Before).ToList();
        var after = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.After).ToList();

        var results = ComputeTests(before, after);
        return results;
    }

    private List<NamedTTestResult> ComputeTests(List<TestCaseStatistics> before, List<TestCaseStatistics> after)
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
                var result = ttest.ExecuteTest(beforeCompiled.RawExecutionResults, afterCompiled.RawExecutionResults);
                results.Add(new NamedTTestResult(testName, result));
            }
        }

        return results;
    }
}