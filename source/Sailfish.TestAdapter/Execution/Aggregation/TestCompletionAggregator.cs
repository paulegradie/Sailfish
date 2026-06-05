using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;

namespace Sailfish.TestAdapter.Execution.Aggregation;

/// <summary>
///     SPIKE: a synchronous, in-process replacement for the test-completion queue subsystem
///     (InMemoryTestCompletionQueue + publisher + consumer + manager + batching service + timeout handler +
///     health check + the per-message MethodComparisonProcessor).
///
///     It does the one job that subsystem actually existed for — buffer the members of a comparison group until
///     the group is whole, then run the cross-method comparison and publish — and nothing else. The behaviour-
///     bearing comparison math is reused untouched: <see cref="MethodComparisonBatchProcessor.ProcessBatch" />.
/// </summary>
/// <remarks>
///     Why this can be synchronous and small (the answer to "are you sure we can delete the queue?"):
///     completions arrive from a sequential in-process <c>foreach … await</c> loop (ClassExecutionDispatcher),
///     the membership of every comparison group is known up front from discovery, and the run has a definite
///     end. So completeness is a deterministic counter — <c>arrived == expected</c> — not the queue's
///     <c>>= 2 successful</c> heuristic that fires <c>ProcessBatch</c> repeatedly and produces the documented
///     double-publish scar tissue (see MethodComparisonProcessor.cs and the #229/#230 regression tests).
///
///     Routing mirrors the queue exactly:
///     <list type="bullet">
///         <item>non-comparison case → published immediately (streamed live), as FrameworkPublishingProcessor did;</item>
///         <item>failed comparison member → published immediately as Failed, and counted toward completion but never buffered;</item>
///         <item>successful comparison member → buffered; when the group is complete it is handed to ProcessBatch once.</item>
///     </list>
///     Stragglers (a sibling that crashed and will never complete) are resolved deterministically at
///     <see cref="FlushAsync" /> — end of run — which is what makes the queue's BatchTimeoutHandler unnecessary.
///
///     Thread-safe via per-group locking so it also covers a future parallel execution path without the queue.
///     A fully serializable message envelope (for a future distributed publisher) is intentionally NOT part of
///     this spike: the metadata dictionary still carries live objects (TestCase, IClassExecutionSummary), so a
///     wire-ready DTO is follow-up work — see ITestCompletionSink for where a distributed publisher would attach.
/// </remarks>
internal sealed class TestCompletionAggregator
{
    private const string ComparisonGroupKey = "ComparisonGroup";

    private readonly MethodComparisonBatchProcessor _comparisonProcessor;
    private readonly ConcurrentDictionary<string, GroupBuffer> _groups = new(StringComparer.Ordinal);
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<ITestCompletionSink> _sinks;

    public TestCompletionAggregator(
        IMediator mediator,
        MethodComparisonBatchProcessor comparisonProcessor,
        ILogger logger,
        IEnumerable<ITestCompletionSink>? sinks = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _comparisonProcessor = comparisonProcessor ?? throw new ArgumentNullException(nameof(comparisonProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sinks = sinks?.ToArray() ?? Array.Empty<ITestCompletionSink>();
    }

    /// <summary>
    ///     Declares how many test cases belong to a comparison group, as known from discovery. This is the
    ///     deterministic completeness signal that lets the comparison fire exactly once, the moment the group is
    ///     whole, instead of guessing from a "&gt;= 2 have shown up" heuristic.
    /// </summary>
    public void RegisterComparisonGroup(string comparisonGroup, int expectedCount)
    {
        var buffer = _groups.GetOrAdd(comparisonGroup, static _ => new GroupBuffer());
        lock (buffer)
        {
            buffer.Expected = expectedCount;
        }
    }

    /// <summary>
    ///     Accepts one completed test case. Non-comparison and failed cases publish immediately; successful
    ///     comparison members are buffered and the comparison is run once the group is complete.
    /// </summary>
    public async Task ReceiveAsync(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        // Extension seam: every observer sees every completion as it lands.
        foreach (var sink in _sinks)
            await sink.OnTestCompletedAsync(message, cancellationToken).ConfigureAwait(false);

        var comparisonGroup = ExtractComparisonGroup(message);
        if (string.IsNullOrEmpty(comparisonGroup))
        {
            // Not part of a comparison — stream it straight through, exactly as the direct path did.
            await PublishImmediately(message, cancellationToken).ConfigureAwait(false);
            return;
        }

        var success = message.TestResult.IsSuccess;
        if (!success)
            // A failed comparison member can never participate in N×N analysis; publish it now as Failed.
            // It still counts toward the group's expected arrivals below so a doomed sibling can't wedge the group.
            await PublishImmediately(message, cancellationToken).ConfigureAwait(false);

        List<TestCompletionQueueMessage>? readyToCompare = null;
        var buffer = _groups.GetOrAdd(comparisonGroup!, static _ => new GroupBuffer());
        lock (buffer)
        {
            buffer.Arrived++;
            if (success) buffer.Successes.Add(message);

            var complete = buffer.Expected > 0 && buffer.Arrived >= buffer.Expected;
            if (complete && !buffer.Processed)
            {
                buffer.Processed = true;
                if (buffer.Successes.Count > 0) readyToCompare = buffer.Successes.ToList();
            }
            else if (buffer.Expected <= 0)
            {
                _logger.Log(LogLevel.Debug,
                    "Comparison group '{0}' has no registered expected count; deferring to end-of-run flush.",
                    comparisonGroup!);
            }
        }

        if (readyToCompare is not null)
            await RunComparison(comparisonGroup!, readyToCompare, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     End of run. Any comparison group that never reached its expected count (e.g. a sibling crashed) is
    ///     resolved here with whatever successful members did arrive — the deterministic replacement for the
    ///     queue's timeout-based draining. Then every sink is told the run is done.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        var leftovers = new List<(string Group, List<TestCompletionQueueMessage> Successes)>();
        foreach (var entry in _groups)
        {
            var buffer = entry.Value;
            lock (buffer)
            {
                if (buffer.Processed) continue;
                buffer.Processed = true;
                if (buffer.Successes.Count > 0) leftovers.Add((entry.Key, buffer.Successes.ToList()));
            }
        }

        foreach (var (group, successes) in leftovers)
            await RunComparison(group, successes, cancellationToken).ConfigureAwait(false);

        foreach (var sink in _sinks)
            await sink.OnRunCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RunComparison(string comparisonGroup, List<TestCompletionQueueMessage> successes, CancellationToken cancellationToken)
    {
        // Hand the complete group to the unchanged comparison processor. It performs the variable-set cohort
        // grouping, baseline-vs-contender / N×N orientation, BH-FDR p-value accumulation, and the enhanced
        // framework republish — exactly as it does today when driven from the queue.
        var className = ExtractClassName(successes[0].TestCaseId);
        var batch = new TestCaseBatch
        {
            BatchId = $"Comparison_{className}_{comparisonGroup}",
            TestCases = successes,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = BatchStatus.Complete
        };

        _logger.Log(LogLevel.Debug,
            "Comparison group '{0}' complete with {1} successful member(s); running comparison once.",
            comparisonGroup, successes.Count);

        await _comparisonProcessor.ProcessBatch(batch, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishImmediately(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        await _mediator.Publish(CreateFrameworkNotification(message), cancellationToken).ConfigureAwait(false);
    }

    private static FrameworkTestCaseEndNotification CreateFrameworkNotification(TestCompletionQueueMessage message)
    {
        // Mirrors MethodComparisonBatchProcessor.CreateFrameworkNotification — the live-object metadata contract
        // both paths share. A serializable envelope would replace this lookup; see the class remarks.
        var formattedMessage = message.Metadata.TryGetValue("FormattedMessage", out var msgObj)
            ? msgObj?.ToString() ?? string.Empty
            : string.Empty;

        var testCase = message.Metadata.TryGetValue("TestCase", out var testCaseObj) && testCaseObj is TestCase originalTestCase
            ? originalTestCase
            : throw new InvalidOperationException($"Original TestCase not found in metadata for test case '{message.TestCaseId}'");

        var startTime = message.Metadata.TryGetValue("StartTime", out var startObj) && startObj is DateTimeOffset start
            ? start
            : message.CompletedAt;
        var endTime = message.Metadata.TryGetValue("EndTime", out var endObj) && endObj is DateTimeOffset end
            ? end
            : message.CompletedAt;

        var statusCode = message.TestResult.IsSuccess ? StatusCode.Success : StatusCode.Failure;
        Exception? exception = !message.TestResult.IsSuccess && !string.IsNullOrEmpty(message.TestResult.ExceptionMessage)
            ? new Exception(message.TestResult.ExceptionMessage)
            : null;

        return new FrameworkTestCaseEndNotification(
            formattedMessage,
            startTime,
            endTime,
            message.PerformanceMetrics.MedianMs,
            testCase,
            statusCode,
            exception);
    }

    private string? ExtractComparisonGroup(TestCompletionQueueMessage message)
        => message.Metadata.TryGetValue(ComparisonGroupKey, out var group) ? group?.ToString() : null;

    private static string ExtractClassName(string testCaseId)
    {
        var lastDot = testCaseId.LastIndexOf('.');
        return lastDot > 0 ? testCaseId[..lastDot] : "Unknown";
    }

    private sealed class GroupBuffer
    {
        public int Expected { get; set; } = -1;
        public int Arrived { get; set; }
        public bool Processed { get; set; }
        public List<TestCompletionQueueMessage> Successes { get; } = new();
    }
}
