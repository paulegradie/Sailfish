using System.Threading;
using System.Threading.Tasks;
using Sailfish.TestAdapter.Comparison;

namespace Sailfish.TestAdapter.Execution.Aggregation;

/// <summary>
///     The extension seam for observing test completions, replacing the queue's chained-processor model.
///     Every completed test case is offered to each registered sink as it lands, and every sink is told once
///     when the run is finished. This is where cross-cutting observers (the old LoggingQueueProcessor, a future
///     Skipper "explain" stage, an artifact/attachment writer, a distributed publisher) plug in — without any
///     of the async producer/consumer, capacity, retry, or health-check machinery the queue carried.
/// </summary>
/// <remarks>
///     A sink is a passive observer: it does not decide when comparison groups are complete (the
///     <see cref="TestCompletionAggregator" /> owns that), it just reacts to the stream. If a future sink needs
///     to do slow background work, a single bounded <c>System.Threading.Channels.Channel&lt;T&gt;</c> can sit
///     behind this same interface — one type, not a subsystem.
/// </remarks>
internal interface ITestCompletionSink
{
    /// <summary>Called once for every test case as it completes, in arrival order.</summary>
    Task OnTestCompletedAsync(TestCompletionMessage message, CancellationToken cancellationToken);

    /// <summary>Called once after the last test case, when the run is being torn down.</summary>
    Task OnRunCompletedAsync(CancellationToken cancellationToken);
}
