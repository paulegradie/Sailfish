using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.Csv;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;
using Sailfish.Utils;

namespace Sailfish.Presentation.TTest;

public class TwoTailedTTestWriter : ITwoTailedTTestWriter
{
    private readonly ITrackingFileFinder trackingFileFinder;
    private readonly IFileIo fileIo;
    private readonly ITTest ttest;
    private readonly IPresentationStringConstructor stringBuilder;

    public TwoTailedTTestWriter(
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

    // TODO: pass in a ttest settings object
    public async Task PresentTestResults(string readDirectory, string outputFilePath, TTestSettings settings)
    {
        await Task.CompletedTask;
        var beforeAndAfter = trackingFileFinder.GetBeforeAndAfterTrackingFiles(readDirectory);
        if (string.IsNullOrEmpty(beforeAndAfter.Before)) return;
        if (string.IsNullOrEmpty(beforeAndAfter.After)) return;

        var results = ComputeTTest(beforeAndAfter, settings);

        var table = results.ToStringTable(
            new List<string>() { "", "ms", "ms", "", "", "", "" },
            m => m.TestName,
            m => m.MeanOfBefore,
            m => m.MeanOfAfter,
            m => m.PValue,
            m => m.DegreesOfFreedom,
            m => m.TStatistic,
            m => m.ChangeDescription
        );
        PrintHeader(
            Path.GetFileName(beforeAndAfter.Before),
            Path.GetFileName(beforeAndAfter.After),
            settings.Alpha);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(table);

        var fileString = stringBuilder.Build();

        System.Console.WriteLine(fileString);

        if (!string.IsNullOrEmpty(fileString))
            await fileIo.WriteToFile(fileString, outputFilePath, CancellationToken.None);
    }

    private void PrintHeader(string beforeId, string afterId, double alpha)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"T-Test results comparing:");
        stringBuilder.AppendLine($"Before: {beforeId}");
        stringBuilder.AppendLine($"After: {afterId}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {alpha}");
    }


    private List<NamedTTestResult> ComputeTTest(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings)
    {
        var before = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.Before).ToList();
        var after = fileIo.ReadCsvFile<TestCaseStatisticMap, TestCaseStatistics>(beforeAndAfter.After).ToList();

        var results = ComputeTests(before, after, settings);
        return results;
    }

    private List<NamedTTestResult> ComputeTests(List<TestCaseStatistics> before, List<TestCaseStatistics> after, TTestSettings testSettings)
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
                var result = ttest.ExecuteTest(beforeCompiled.RawExecutionResults, afterCompiled.RawExecutionResults, testSettings);
                results.Add(new NamedTTestResult(testName, result));
            }
        }

        return results;
    }
}