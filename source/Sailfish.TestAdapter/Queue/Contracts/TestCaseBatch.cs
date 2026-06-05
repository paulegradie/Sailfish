using System;
using System.Collections.Generic;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
///     A group of related test-completion messages handed to
///     <see cref="Processors.MethodComparison.MethodComparisonBatchProcessor" /> for cross-method (N×N) analysis.
///     Formerly produced by the queue's batching service; now assembled by
///     <see cref="Execution.Aggregation.TestCompletionAggregator" /> once a comparison group is complete.
/// </summary>
internal class TestCaseBatch
{
    /// <summary>Unique identifier for this batch (used only for diagnostics).</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>The test-completion messages belonging to this comparison group.</summary>
    public List<TestCompletionQueueMessage> TestCases { get; set; } = new();

    /// <summary>The batch status. Retained for compatibility with the comparison processor and its tests.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    /// <summary>When the batch was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the batch was completed, or null if not complete.</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>The lifecycle status of a <see cref="TestCaseBatch" />.</summary>
internal enum BatchStatus
{
    Pending = 0,
    Complete = 1,
    Processing = 2,
    Processed = 3,
    TimedOut = 4,
    Error = 5
}
