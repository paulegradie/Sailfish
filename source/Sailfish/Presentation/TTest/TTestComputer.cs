using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Statistics.StatisticalAnalysis;
using Sailfish.Utils;
using Serilog;

namespace Sailfish.Presentation.TTest;

internal class TTestComputer : ITTestComputer
{
    private readonly IFileIo fileIo;
    private readonly ITTest tTest;
    private readonly ILogger logger;

    public TTestComputer(IFileIo fileIo, ITTest tTest, ILogger logger)
    {
        this.fileIo = fileIo;
        this.tTest = tTest;
        this.logger = logger;
    }

    public List<NamedTTestResult> ComputeTTest(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings)
    {
        try
        {
            var before = fileIo.ReadCsvFile<TestCaseDescriptiveStatisticsMap, DescriptiveStatisticsResult>(beforeAndAfter.BeforeFilePath);
            var after = fileIo.ReadCsvFile<TestCaseDescriptiveStatisticsMap, DescriptiveStatisticsResult>(beforeAndAfter.AfterFilePath).ToList();
            var results = Compute(before, after, settings);
            return results;
        }
        catch (Exception ex)
        {
            logger.Fatal("Unable to read tracking files before and after: {Message}", ex.Message);
            return new List<NamedTTestResult>();
        }
    }

    private List<NamedTTestResult> Compute(List<DescriptiveStatisticsResult> before, List<DescriptiveStatisticsResult> after, TTestSettings testSettings)
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