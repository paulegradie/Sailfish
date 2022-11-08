using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Contracts.Public;
using Sailfish.MathOps;
using Serilog;

namespace Sailfish.Analysis;

internal class TestComputer : ITestComputer
{
    private readonly IStatisticalTestExecutor statisticalTestExecutor;
    private readonly ILogger logger;

    public TestComputer(IStatisticalTestExecutor statisticalTestExecutor, ILogger logger)
    {
        this.statisticalTestExecutor = statisticalTestExecutor;
        this.logger = logger;
    }

    public List<TestCaseResults> ComputeTest(TestData before, TestData after, TestSettings settings)
    {
        List<string> testNames;
        try
        {
            var rawNames = after.Data.Select(x => x.DisplayName);
            testNames = rawNames
                .OrderBy(FirstTestName)
                .ThenBy(SecondTestName)
                .ThenBy(ThirdTestName)
                .ThenBy(FirstNumberRetriever)
                .ThenBy(SecondNumberRetriever)
                .ThenBy(ThirdNumberRetriever)
                .ToList();
        }
        catch
        {
            testNames = after.Data.Select(x => x.DisplayName).OrderByDescending(x => x).ToList();
        }

        var results = new List<TestCaseResults>();
        foreach (var testName in testNames.Distinct())
        {
            var afterCompiled = Aggregate(
                testName,
                settings,
                after
                    .Data
                    .Where(x => x.DisplayName.Equals(testName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList());

            var beforeCompiled = Aggregate(
                testName,
                settings,
                before
                    .Data
                    .Where(x => x.DisplayName.Equals(testName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList());

            if (beforeCompiled is null || afterCompiled is null) continue; // this could be the first time we've ever run this test

            var result = statisticalTestExecutor.ExecuteStatisticalTest(
                beforeCompiled.RawExecutionResults,
                afterCompiled.RawExecutionResults,
                settings);

            results.Add(new TestCaseResults(testName, result));
        }

        return results;
    }

    private static DescriptiveStatisticsResult? Aggregate(string displayName, TestSettings settings, IReadOnlyCollection<DescriptiveStatisticsResult> data)
    {
        switch (data.Count)
        {
            case 0:
                return null;
            case 1:
                return data.Single();
            default:
                var allRawData = data.SelectMany(x => x.RawExecutionResults).ToArray();
                return new DescriptiveStatisticsResult
                {
                    RawExecutionResults = allRawData,
                    DisplayName = displayName,
                    GlobalDuration = data.Select(x => x.GlobalDuration).Mean(),
                    GlobalStart = data.OrderBy(x => x.GlobalStart).First().GlobalStart,
                    GlobalEnd = data.OrderBy(x => x.GlobalEnd).First().GlobalEnd,
                    Mean = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Mean() : allRawData.Mean(),
                    Median = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Median() : allRawData.Median(),
                    Variance = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Variance() : allRawData.Variance(),
                    StdDev = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).StandardDeviation() : allRawData.StandardDeviation(),
                };
        }
    }

    private static string? FirstTestName(string s)
    {
        return GetNamePart(s, 0);
    }

    private static string? SecondTestName(string s)
    {
        return GetNamePart(s, 1);
    }

    private static string? ThirdTestName(string s)
    {
        return GetNamePart(s, 2);
    }

    private static string? GetNamePart(string s, int i)
    {
        try
        {
            return s.Split("(").First().Split(".").ToList()[i];
        }
        catch
        {
            return null;
        }
    }

// name will be like
// some.test(maybe:20,other:30)
// some.test(maybe:10,other:30)
    private static int? FirstNumberRetriever(string s)
    {
        var elements = GetElements(s);
        return elements.FirstOrDefault();
    }

    private static int? SecondNumberRetriever(string s)
    {
        var elements = GetElements(s);
        try
        {
            return elements[1];
        }
        catch
        {
            return null;
        }
    }

    private static int? ThirdNumberRetriever(string s)
    {
        var elements = GetElements(s);
        try
        {
            return elements[2];
        }
        catch
        {
            return null;
        }
    }

    private static List<int> GetElements(string s)
    {
        var elements = s
            .Split("(")
            .Last()
            .Replace(")", string.Empty)
            .Split(",")
            .Select(x => x.Split(":").Last())
            .Select(int.Parse)
            .ToList();
        return elements;
    }
}