using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

public class MethodComparisonBatchProcessor_AccumulateAndPublishTests
{
    private readonly IAdapterSailDiff _sailDiff = Substitute.For<IAdapterSailDiff>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly ISailDiffUnifiedFormatter _formatter = Substitute.For<ISailDiffUnifiedFormatter>();

    private MethodComparisonBatchProcessor CreateSut()
    {
        return new MethodComparisonBatchProcessor(_sailDiff, _mediator, _logger, _formatter);
    }

    private static TestCompletionQueueMessage CreateMessage(string className, string methodName, string group, double meanMs)
    {
        var fqn = $"{className}.{methodName}";
        var testCase = new TestCase(fqn, new Uri("executor://sailfish"), "Sailfish");
        return new TestCompletionQueueMessage
        {
            TestCaseId = fqn,
            TestResult = new TestExecutionResult { IsSuccess = true },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["ComparisonGroup"] = group,
                ["TestCase"] = testCase,
                ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1),
                ["EndTime"] = DateTimeOffset.UtcNow,
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = meanMs,
                MedianMs = meanMs,
                SampleSize = 5,
                StandardDeviation = 1.0,
                Variance = 1.0,
                RawExecutionResults = new[] { meanMs - 1, meanMs, meanMs + 1 },
                DataWithOutliersRemoved = new[] { meanMs },
                UpperOutliers = Array.Empty<double>(),
                LowerOutliers = Array.Empty<double>(),
                TotalNumOutliers = 0,
                GroupingId = group,
                NumWarmupIterations = 0
            }
        };
    }

    private static TestCaseSailDiffResult CreateFakeDiff()
    {
        var stat = new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 12.0,
            medianBefore: 9.5,
            medianAfter: 11.5,
            testStatistic: 2.0,
            pValue: 0.04,
            changeDescription: "NoChange",
            sampleSizeBefore: 5,
            sampleSizeAfter: 5,
            rawDataBefore: new[] { 9.0, 10.0, 11.0 },
            rawDataAfter: new[] { 11.0, 12.0, 13.0 },
            additionalResults: new Dictionary<string, object>());

        var withOutliers = new TestResultWithOutlierAnalysis(stat,
            new ProcessedStatisticalTestData(new[] { 9.0, 10.0, 11.0 }, new[] { 9.0, 10.0, 11.0 }, Array.Empty<double>(), Array.Empty<double>(), 0),
            new ProcessedStatisticalTestData(new[] { 11.0, 12.0, 13.0 }, new[] { 11.0, 12.0, 13.0 }, Array.Empty<double>(), Array.Empty<double>(), 0));

        var diff = new Sailfish.Contracts.Public.Models.SailDiffResult(new TestCaseId("Dummy"), withOutliers);
        return new TestCaseSailDiffResult(new List<Sailfish.Contracts.Public.Models.SailDiffResult> { diff }, new TestIds(new[] { "A" }, new[] { "B" }), new Sailfish.Analysis.SailDiff.SailDiffSettings());
    }

    private static void AttachClassExecutionSummary(params TestCompletionQueueMessage[] messages)
    {
        // Build a minimal IClassExecutionSummary with compiled results for all provided messages
        var compiled = new List<ICompiledTestCaseResult>();
        foreach (var m in messages)
        {
            var pm = m.PerformanceMetrics;
            var clean = pm.DataWithOutliersRemoved ?? pm.RawExecutionResults ?? Array.Empty<double>();
            var n = clean.Length;
            var se = n > 1 ? pm.StandardDeviation / Math.Sqrt(n) : 0;
            var pr = new PerformanceRunResult(
                m.TestCaseId,
                pm.MeanMs,
                pm.StandardDeviation,
                pm.Variance,
                pm.MedianMs,
                pm.RawExecutionResults ?? Array.Empty<double>(),
                pm.SampleSize,
                pm.NumWarmupIterations,
                clean,
                pm.UpperOutliers ?? Array.Empty<double>(),
                pm.LowerOutliers ?? Array.Empty<double>(),
                pm.TotalNumOutliers,
                se,
                0.95,
                pm.MeanMs,
                pm.MeanMs,
                0.0);

            compiled.Add(new StubCompiledResult(new TestCaseId(m.TestCaseId), pm.GroupingId ?? string.Empty, pr));
        }

        var summary = new StubClassExecutionSummary(typeof(object), new ExecutionSettings(), compiled);
        foreach (var m in messages)
        {
            m.Metadata["ClassExecutionSummaries"] = summary;
        }
    }

    private sealed class StubCompiledResult : ICompiledTestCaseResult
    {
        public StubCompiledResult(TestCaseId id, string grouping, PerformanceRunResult pr)
        {
            TestCaseId = id;
            GroupingId = grouping;
            PerformanceRunResult = pr;
        }
        public string? GroupingId { get; }
        public PerformanceRunResult? PerformanceRunResult { get; }
        public Exception? Exception { get; } = null;
        public TestCaseId? TestCaseId { get; }
    }

    private sealed class StubClassExecutionSummary : IClassExecutionSummary
    {
        public StubClassExecutionSummary(Type type, IExecutionSettings settings, IEnumerable<ICompiledTestCaseResult> results)
        {
            TestClass = type;
            ExecutionSettings = settings;
            CompiledTestCaseResults = results;
        }
        public Type TestClass { get; }
        public IExecutionSettings ExecutionSettings { get; }
        public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }
        public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases() => CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
        public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases() => CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
        public IClassExecutionSummary FilterForSuccessfulTestCases() => new StubClassExecutionSummary(TestClass, ExecutionSettings, GetSuccessfulTestCases());
        public IClassExecutionSummary FilterForFailureTestCases() => new StubClassExecutionSummary(TestClass, ExecutionSettings, GetFailedTestCases());
    }

    [Fact]
    public async Task ProcessBatch_WithTwoMethods_AccumulatesAndPublishesOncePerMethod()
    {
        // Arrange
        var sut = CreateSut();

        _sailDiff.ComputeTestCaseDiff(default!, default!, default!, default!, default!)
            .ReturnsForAnyArgs(CreateFakeDiff());

        _formatter.Format(default!, default!)
            .ReturnsForAnyArgs(new Sailfish.Analysis.SailDiff.Formatting.SailDiffFormattedOutput
            {
                FullOutput = "\n📊 COMPARISON RESULTS: MOCK\n"
            });

        var m1 = CreateMessage("TestClass1", "BeforeMethod", "GroupX", 10.0);
        var m2 = CreateMessage("TestClass1", "AfterMethod", "GroupX", 12.0);

        // Provide the class execution summary expected by the batch processor
        AttachClassExecutionSummary(m1, m2);

        var batch = new TestCaseBatch
        {
            BatchId = "Comparison_TestClass1_GroupX",
            TestCases = new List<TestCompletionQueueMessage> { m1, m2 },
            Status = BatchStatus.Complete,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        };

        // Act
        await sut.ProcessBatch(batch, CancellationToken.None);

        // Assert: mediator published for each test
        await _mediator.Received(2).Publish(Arg.Is<FrameworkTestCaseEndNotification>(n =>
            n.TestOutputWindowMessage.Contains("COMPARISON RESULTS: MOCK")
            && n.TestCase.FullyQualifiedName.StartsWith("TestClass1.")
        ), Arg.Any<CancellationToken>());

        // Assert: accumulated comparisons stored on each message
        m1.Metadata.ShouldContainKey("AccumulatedComparisons");
        m2.Metadata.ShouldContainKey("AccumulatedComparisons");
        ((List<string>)m1.Metadata["AccumulatedComparisons"]).Count.ShouldBe(1);
        ((List<string>)m2.Metadata["AccumulatedComparisons"]).Count.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessBatch_WithThreeMethods_AccumulatesMultipleComparisonsPerMethod()
    {
        // Arrange
        var sut = CreateSut();
        _sailDiff.ComputeTestCaseDiff(default!, default!, default!, default!, default!)
            .ReturnsForAnyArgs(CreateFakeDiff());
        _formatter.Format(default!, default!)
            .ReturnsForAnyArgs(new Sailfish.Analysis.SailDiff.Formatting.SailDiffFormattedOutput
            {
                FullOutput = "\n📊 COMPARISON RESULTS: MOCK\n"
            });

        var a = CreateMessage("TestClass1", "A", "GroupY", 10);
        var b = CreateMessage("TestClass1", "B", "GroupY", 12);
        var c = CreateMessage("TestClass1", "C", "GroupY", 11);

        // Provide the class execution summary expected by the batch processor
        AttachClassExecutionSummary(a, b, c);

        var batch = new TestCaseBatch
        {
            BatchId = "Comparison_TestClass1_GroupY",
            TestCases = new List<TestCompletionQueueMessage> { a, b, c },
            Status = BatchStatus.Complete,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        };

        // Act
        await sut.ProcessBatch(batch, CancellationToken.None);

        // Assert: 3 notifications for 3 methods
        await _mediator.Received(3).Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());

        // Each method compared to 2 others -> 2 accumulated comparisons per method
        ((List<string>)a.Metadata["AccumulatedComparisons"]).Count.ShouldBe(2);
        ((List<string>)b.Metadata["AccumulatedComparisons"]).Count.ShouldBe(2);
        ((List<string>)c.Metadata["AccumulatedComparisons"]).Count.ShouldBe(2);
    }
}