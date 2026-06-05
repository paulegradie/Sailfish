using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Comparison;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Execution.Aggregation;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Aggregation;

/// <summary>
///     Tests for <see cref="TestCompletionAggregator" />: routing (stream non-comparison/failed immediately,
///     buffer successful comparison members), exactly-once firing by known count, end-of-run flush, the sink
///     seam, and the TestExecutor comparison-group seeding that feeds it. Drives the real
///     <see cref="MethodComparisonBatchProcessor" />.
/// </summary>
public class TestCompletionAggregatorTests
{
    private readonly MethodComparisonBatchProcessor _batchProcessor;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IAdapterSailDiff _sailDiff;
    private readonly ISailDiffUnifiedFormatter _unifiedFormatter;

    public TestCompletionAggregatorTests()
    {
        _mediator = Substitute.For<IMediator>();
        _sailDiff = Substitute.For<IAdapterSailDiff>();
        _unifiedFormatter = Substitute.For<ISailDiffUnifiedFormatter>();
        _logger = Substitute.For<ILogger>();

        // The REAL comparison processor — the behaviour-bearing code we are preserving. It and the aggregator
        // share one mediator so every framework publish (immediate or enhanced) is counted in one place.
        _batchProcessor = new MethodComparisonBatchProcessor(_sailDiff, _mediator, _logger, _unifiedFormatter);
    }

    private TestCompletionAggregator NewAggregator(params ITestCompletionSink[] sinks)
        => new(_mediator, _batchProcessor, _logger, sinks);

    [Fact]
    public async Task NonComparisonMessage_IsStreamedImmediately()
    {
        var aggregator = NewAggregator();

        await aggregator.ReceiveAsync(CreateMessage("Solo", comparisonGroup: null), CancellationToken.None);

        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("Solo")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComparisonGroup_IsHeldUntilComplete_ThenEachMemberPublishedOnce()
    {
        var aggregator = NewAggregator();
        aggregator.RegisterComparisonGroup("G", expectedCount: 2);

        // First member arrives — group is not yet whole, so nothing is published.
        await aggregator.ReceiveAsync(CreateMessage("MethodA", "G"), CancellationToken.None);
        await _mediator.DidNotReceive().Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());

        // Second member completes the group — the comparison fires and both members publish, once each.
        await aggregator.ReceiveAsync(CreateMessage("MethodB", "G"), CancellationToken.None);
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodA")),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodB")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThreeMemberGroup_DoesNotFireAtTwo_FiresExactlyOnceAtThree()
    {
        // This is the headline: the queue fired ProcessBatch on a ">= 2 successful" heuristic, which
        // double-published the early members of a 3+ group (see MethodComparisonProcessor #229/#230 scar
        // tissue). A known-count aggregator fires once, when the group is actually whole.
        var aggregator = NewAggregator();
        aggregator.RegisterComparisonGroup("G", expectedCount: 3);

        await aggregator.ReceiveAsync(CreateMessage("MethodA", "G"), CancellationToken.None);
        await aggregator.ReceiveAsync(CreateMessage("MethodB", "G"), CancellationToken.None);

        // Two of three present — the heuristic would have fired here. The aggregator must not.
        await _mediator.DidNotReceive().Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());

        await aggregator.ReceiveAsync(CreateMessage("MethodC", "G"), CancellationToken.None);

        foreach (var method in new[] { "MethodA", "MethodB", "MethodC" })
            await _mediator.Received(1).Publish(
                Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith(method)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedComparisonMember_IsPublishedImmediatelyAndDoesNotWedgeTheGroup()
    {
        var aggregator = NewAggregator();
        aggregator.RegisterComparisonGroup("G", expectedCount: 2);

        // A failed member can't take part in N×N — it publishes immediately as Failed, but still counts toward
        // completion so its one surviving sibling isn't left waiting forever.
        await aggregator.ReceiveAsync(CreateMessage("FailA", "G", success: false), CancellationToken.None);
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n =>
                n.TestCase.FullyQualifiedName.EndsWith("FailA") && n.StatusCode == StatusCode.Failure),
            Arg.Any<CancellationToken>());

        await aggregator.ReceiveAsync(CreateMessage("PassB", "G"), CancellationToken.None);
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n =>
                n.TestCase.FullyQualifiedName.EndsWith("PassB") && n.StatusCode == StatusCode.Success),
            Arg.Any<CancellationToken>());

        // The failed member is never republished by the comparison path.
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("FailA")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncompleteGroup_IsResolvedDeterministicallyAtFlush()
    {
        // Replaces the queue's BatchTimeoutHandler: a group whose sibling never arrives is processed at end of
        // run with whatever did arrive — no timeout, no "produced no requests" guesswork.
        var aggregator = NewAggregator();
        aggregator.RegisterComparisonGroup("G", expectedCount: 3);

        await aggregator.ReceiveAsync(CreateMessage("MethodA", "G"), CancellationToken.None);
        await aggregator.ReceiveAsync(CreateMessage("MethodB", "G"), CancellationToken.None);
        await _mediator.DidNotReceive().Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());

        await aggregator.FlushAsync(CancellationToken.None);

        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodA")),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodB")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sinks_ObserveEveryCompletionAndTheRunCompletion()
    {
        // The extension seam survives: an observer sees every completion and the end-of-run signal, with none
        // of the queue's processor/consumer/manager lifecycle. This is where LoggingQueueProcessor (or a future
        // Skipper "explain" / attachment / distributed-publish stage) plugs in.
        var sink = new CountingSink();
        var aggregator = NewAggregator(sink);
        aggregator.RegisterComparisonGroup("G", expectedCount: 2);

        await aggregator.ReceiveAsync(CreateMessage("Solo", comparisonGroup: null), CancellationToken.None);
        await aggregator.ReceiveAsync(CreateMessage("MethodA", "G"), CancellationToken.None);
        await aggregator.ReceiveAsync(CreateMessage("MethodB", "G"), CancellationToken.None);
        await aggregator.FlushAsync(CancellationToken.None);

        sink.Completed.ShouldBe(3);
        sink.RunCompleted.ShouldBe(1);
    }

    [Fact]
    public async Task SeededFromTestCases_FiresComparisonAtTheDiscoveredCount()
    {
        // Validates the TestExecutor seam: SeedComparisonGroups derives each group's expected count from the
        // discovered TestCases (via the comparison-group property), so the comparison fires exactly once when
        // that many members complete — and ungrouped cases are ignored by seeding.
        var aggregator = NewAggregator();
        var testCases = new List<TestCase>
        {
            ComparisonTestCase("MethodA", "G"),
            ComparisonTestCase("MethodB", "G"),
            new("TestClass1.Solo", new Uri("executor://sailfishexecutor/v1"), "Sailfish")
        };

        TestExecutor.SeedComparisonGroups(aggregator, testCases);

        await aggregator.ReceiveAsync(CreateMessage("MethodA", "G"), CancellationToken.None);
        await _mediator.DidNotReceive().Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());

        await aggregator.ReceiveAsync(CreateMessage("MethodB", "G"), CancellationToken.None);
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodA")),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase.FullyQualifiedName.EndsWith("MethodB")),
            Arg.Any<CancellationToken>());
    }

    private static TestCase ComparisonTestCase(string method, string comparisonGroup)
    {
        var testCase = new TestCase($"TestClass1.{method}", new Uri("executor://sailfishexecutor/v1"), "Sailfish");
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishComparisonGroupProperty, comparisonGroup);
        return testCase;
    }

    private static TestCompletionMessage CreateMessage(string method, string? comparisonGroup, bool success = true)
    {
        var fullyQualifiedName = $"TestClass1.{method}";
        var testCase = new TestCase(fullyQualifiedName, new Uri("executor://sailfishexecutor/v1"), "Sailfish");

        var metadata = new Dictionary<string, object>
        {
            ["TestCase"] = testCase,
            ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1),
            ["EndTime"] = DateTimeOffset.UtcNow
        };
        if (comparisonGroup != null) metadata["ComparisonGroup"] = comparisonGroup;

        return new TestCompletionMessage
        {
            TestCaseId = fullyQualifiedName,
            CompletedAt = DateTime.UtcNow,
            TestResult = new TestExecutionResult
            {
                IsSuccess = success,
                ExceptionMessage = success ? null : "boom"
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = 100.0,
                MedianMs = 98.0,
                StandardDeviation = 5.0,
                Variance = 25.0,
                SampleSize = 10,
                RawExecutionResults = new double[] { 95, 100, 105 },
                DataWithOutliersRemoved = new double[] { 100 }
            },
            Metadata = metadata
        };
    }

    private sealed class CountingSink : ITestCompletionSink
    {
        public int Completed { get; private set; }
        public int RunCompleted { get; private set; }

        public Task OnTestCompletedAsync(TestCompletionMessage message, CancellationToken cancellationToken)
        {
            Completed++;
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(CancellationToken cancellationToken)
        {
            RunCompleted++;
            return Task.CompletedTask;
        }
    }
}
