using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class StatisticalTestComputerTests
{
    private readonly IStatisticalTestExecutor _mockStatisticalTestExecutor;
    private readonly IPerformanceRunResultAggregator _mockAggregator;
    private readonly StatisticalTestComputer _computer;

    public StatisticalTestComputerTests()
    {
        _mockStatisticalTestExecutor = Substitute.For<IStatisticalTestExecutor>();
        _mockAggregator = Substitute.For<IPerformanceRunResultAggregator>();
        _computer = new StatisticalTestComputer(_mockStatisticalTestExecutor, _mockAggregator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var computer = new StatisticalTestComputer(_mockStatisticalTestExecutor, _mockAggregator);

        // Assert
        computer.ShouldNotBeNull();
    }

    #endregion

    #region Basic Computation Tests

    [Fact]
    public void ComputeTest_WithValidData_ShouldReturnResults()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]);
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedBefore, aggregatedAfter);

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(1);
        results[0].TestCaseId.DisplayName.ShouldBe(testCaseId.DisplayName);
    }

    [Fact]
    public void ComputeTest_WithMultipleTestCases_ShouldReturnMultipleResults()
    {
        // Arrange
        var testCaseId1 = new TestCaseId("TestClass.TestMethod1");
        var testCaseId2 = new TestCaseId("TestClass.TestMethod2");

        var beforeResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0])
        };

        var afterResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [3.0, 4.0, 5.0, 6.0, 7.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [4.0, 5.0, 6.0, 7.0, 8.0])
        };

        var beforeData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName], beforeResults);
        var afterData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName], afterResults);
        var settings = new SailDiffSettings();

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);
    }

    #endregion

    #region Aggregation Tests

    [Fact]
    public void ComputeTest_ShouldAggregateBeforeAndAfterData()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]);
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedBefore, aggregatedAfter);

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        _mockAggregator.Received(2).Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>());
    }

    [Fact]
    public void ComputeTest_WithNullAggregatedBefore_ShouldSkipTestCase()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0]);
        var settings = new SailDiffSettings();

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(null, CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0, 4.0]));

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldBeEmpty();
        _mockStatisticalTestExecutor.DidNotReceive().ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public void ComputeTest_WithNullAggregatedAfter_ShouldSkipTestCase()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0]);
        var settings = new SailDiffSettings();

        // The aggregator is called twice: first for afterData, then for beforeData
        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(null, CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0, 3.0]));

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldBeEmpty();
        _mockStatisticalTestExecutor.DidNotReceive().ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>());
    }

    #endregion

    #region Sample Size Validation Tests

    [Fact]
    public void ComputeTest_WithInsufficientBeforeSampleSize_ShouldSkipTestCase()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0]); // Only 2 samples
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0]);
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0]);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0]);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedBefore, aggregatedAfter);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldBeEmpty();
        _mockStatisticalTestExecutor.DidNotReceive().ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public void ComputeTest_WithInsufficientAfterSampleSize_ShouldSkipTestCase()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0]); // Only 2 samples
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0]);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0]);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedBefore, aggregatedAfter);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldBeEmpty();
        _mockStatisticalTestExecutor.DidNotReceive().ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public void ComputeTest_WithExactlyThreeSamples_ShouldProcessTestCase()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0]);
        var afterData = CreateTestData(testCaseId.DisplayName, [2.0, 3.0, 4.0]);
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, [1.0, 2.0, 3.0]);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, [2.0, 3.0, 4.0]);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedBefore, aggregatedAfter);

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(1);
        _mockStatisticalTestExecutor.Received(1).ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>());
    }

    #endregion

    #region Statistical Test Execution Tests

    [Fact]
    public void ComputeTest_ShouldPassCorrectDataToStatisticalTestExecutor()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeRawData = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var afterRawData = new[] { 2.0, 3.0, 4.0, 5.0, 6.0 };

        var beforeData = CreateTestData(testCaseId.DisplayName, beforeRawData);
        var afterData = CreateTestData(testCaseId.DisplayName, afterRawData);
        var settings = new SailDiffSettings();

        var aggregatedBefore = CreateAggregatedResult(testCaseId.DisplayName, beforeRawData);
        var aggregatedAfter = CreateAggregatedResult(testCaseId.DisplayName, afterRawData);

        // The aggregator is called twice: first for afterData, then for beforeData
        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(aggregatedAfter, aggregatedBefore);

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        _mockStatisticalTestExecutor.Received(1).ExecuteStatisticalTest(
            Arg.Is<double[]>(x => x.SequenceEqual(beforeRawData)),
            Arg.Is<double[]>(x => x.SequenceEqual(afterRawData)),
            Arg.Is<SailDiffSettings>(x => x == settings));
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void ComputeTest_WithOrderingEnabled_ShouldReturnOrderedResults()
    {
        // Arrange
        var testCaseId1 = new TestCaseId("TestClass.ZMethod");
        var testCaseId2 = new TestCaseId("TestClass.AMethod");
        var testCaseId3 = new TestCaseId("TestClass.MMethod");

        var beforeResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]),
            CreatePerformanceRunResult(testCaseId3.DisplayName, [3.0, 4.0, 5.0, 6.0, 7.0])
        };

        var afterResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [3.0, 4.0, 5.0, 6.0, 7.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [4.0, 5.0, 6.0, 7.0, 8.0]),
            CreatePerformanceRunResult(testCaseId3.DisplayName, [5.0, 6.0, 7.0, 8.0, 9.0])
        };

        var beforeData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName, testCaseId3.DisplayName], beforeResults);
        var afterData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName, testCaseId3.DisplayName], afterResults);
        var settings = new SailDiffSettings(disableOrdering: false);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(3);
        // TestCaseId adds "()" to display names when there are no variables
        results[0].TestCaseId.DisplayName.ShouldBe("TestClass.AMethod()");
        results[1].TestCaseId.DisplayName.ShouldBe("TestClass.MMethod()");
        results[2].TestCaseId.DisplayName.ShouldBe("TestClass.ZMethod()");
    }

    [Fact]
    public void ComputeTest_WithOrderingDisabled_ShouldReturnUnorderedResults()
    {
        // Arrange
        var testCaseId1 = new TestCaseId("TestClass.ZMethod");
        var testCaseId2 = new TestCaseId("TestClass.AMethod");

        var beforeResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0])
        };

        var afterResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId1.DisplayName, [3.0, 4.0, 5.0, 6.0, 7.0]),
            CreatePerformanceRunResult(testCaseId2.DisplayName, [4.0, 5.0, 6.0, 7.0, 8.0])
        };

        var beforeData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName], beforeResults);
        var afterData = new TestData([testCaseId1.DisplayName, testCaseId2.DisplayName], afterResults);
        var settings = new SailDiffSettings(disableOrdering: true);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(2);
        // Results should not be ordered - we just verify we got results back
        results.ShouldNotBeNull();
    }

    [Fact]
    public void ComputeTest_WithMoreThan60Results_ShouldReturnUnorderedResults()
    {
        // Arrange
        var testCaseIds = Enumerable.Range(1, 65).Select(i => new TestCaseId($"TestClass.Method{i:D3}")).ToList();

        var beforeResults = testCaseIds.Select(id =>
            CreatePerformanceRunResult(id.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0])).ToList();

        var afterResults = testCaseIds.Select(id =>
            CreatePerformanceRunResult(id.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0])).ToList();

        var beforeData = new TestData(testCaseIds.Select(x => x.DisplayName), beforeResults);
        var afterData = new TestData(testCaseIds.Select(x => x.DisplayName), afterResults);
        var settings = new SailDiffSettings(disableOrdering: false);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(65);
        // With more than 60 results, ordering should be skipped
    }

    #endregion

    #region Parallelism Tests

    [Fact]
    public void ComputeTest_WithMaxDegreeOfParallelism_ShouldRespectSetting()
    {
        // Arrange
        var testCaseIds = Enumerable.Range(1, 10).Select(i => new TestCaseId($"TestClass.Method{i}")).ToList();

        var beforeResults = testCaseIds.Select(id =>
            CreatePerformanceRunResult(id.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0])).ToList();

        var afterResults = testCaseIds.Select(id =>
            CreatePerformanceRunResult(id.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0])).ToList();

        var beforeData = new TestData(testCaseIds.Select(x => x.DisplayName), beforeResults);
        var afterData = new TestData(testCaseIds.Select(x => x.DisplayName), afterResults);
        var settings = new SailDiffSettings(maxDegreeOfParallelism: 2);

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(10);
        // All test cases should be processed despite parallelism setting
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ComputeTest_WithEmptyAfterData_ShouldReturnEmptyResults()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");
        var beforeData = CreateTestData(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]);
        var afterData = new TestData([], []);
        var settings = new SailDiffSettings();

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void ComputeTest_WithDuplicateTestCaseIds_ShouldProcessOnlyUnique()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod");

        var beforeResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId.DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]),
            CreatePerformanceRunResult(testCaseId.DisplayName, [1.5, 2.5, 3.5, 4.5, 5.5])
        };

        var afterResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult(testCaseId.DisplayName, [2.0, 3.0, 4.0, 5.0, 6.0]),
            CreatePerformanceRunResult(testCaseId.DisplayName, [2.5, 3.5, 4.5, 5.5, 6.5])
        };

        var beforeData = new TestData([testCaseId.DisplayName, testCaseId.DisplayName], beforeResults);
        var afterData = new TestData([testCaseId.DisplayName, testCaseId.DisplayName], afterResults);
        var settings = new SailDiffSettings();

        _mockAggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => CreateAggregatedResult(((TestCaseId)x[0]).DisplayName, [1.0, 2.0, 3.0, 4.0, 5.0]));

        var testResult = CreateTestResultWithOutlierAnalysis();
        _mockStatisticalTestExecutor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(testResult);

        // Act
        var results = _computer.ComputeTest(beforeData, afterData, settings);

        // Assert
        results.Count.ShouldBe(1); // Should only process unique test case IDs
    }

    #endregion

    #region Helper Methods

    private TestData CreateTestData(string displayName, double[] rawData)
    {
        var performanceResult = CreatePerformanceRunResult(displayName, rawData);
        return new TestData([displayName], [performanceResult]);
    }

    private PerformanceRunResult CreatePerformanceRunResult(string displayName, double[] rawData)
    {
        return PerformanceRunResultBuilder.Create()
            .WithDisplayName(displayName)
            .WithRawExecutionResults(rawData)
            .WithSampleSize(rawData.Length)
            .WithMean(rawData.Average())
            .WithMedian(rawData.OrderBy(x => x).ElementAt(rawData.Length / 2))
            .WithStdDev(1.0)
            .WithVariance(1.0)
            .WithNumWarmupIterations(3)
            .WithDataWithOutliersRemoved(rawData)
            .WithUpperOutliers([])
            .WithLowerOutliers([])
            .WithTotalNumOutliers(0)
            .Build();
    }

    private AggregatedPerformanceResult CreateAggregatedResult(string displayName, double[] rawData)
    {
        var testCaseId = new TestCaseId(displayName);
        var performanceResult = CreatePerformanceRunResult(displayName, rawData);
        return AggregatedPerformanceResult.Aggregate(testCaseId, [performanceResult]);
    }

    private TestResultWithOutlierAnalysis CreateTestResultWithOutlierAnalysis()
    {
        var statisticalResult = new StatisticalTestResult(
            meanBefore: 3.0,
            meanAfter: 4.0,
            medianBefore: 3.0,
            medianAfter: 4.0,
            testStatistic: 2.5,
            pValue: 0.05,
            changeDescription: "NoChange",
            sampleSizeBefore: 5,
            sampleSizeAfter: 5,
            rawDataBefore: [1.0, 2.0, 3.0, 4.0, 5.0],
            rawDataAfter: [2.0, 3.0, 4.0, 5.0, 6.0],
            additionalResults: new Dictionary<string, object>());

        return new TestResultWithOutlierAnalysis(statisticalResult, null, null);
    }

    #endregion
}

