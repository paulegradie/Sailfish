using System.Threading;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Records whether the Skipper AI layer actually engaged this run — i.e. a SailDiff, ScaleFish, or
///     method-comparison trigger reached a real agent with analyzable content. The adapter reads this after the
///     run to distinguish "AI ran" from "AI was enabled but never had anything to act on", so it can emit one
///     actionable warning instead of leaving the user staring at silence (the core UX failure this addresses).
///     Registered as a singleton; the DI container is built once per run, so the flag is naturally run-scoped
///     and needs no reset.
/// </summary>
internal interface ISkipperActivitySink
{
    /// <summary>Marks that Skipper engaged at least once this run.</summary>
    void RecordTriggered();

    /// <summary>True once any Skipper analysis (SailDiff / ScaleFish / method comparison) has engaged.</summary>
    bool Triggered { get; }
}

/// <inheritdoc cref="ISkipperActivitySink" />
internal sealed class SkipperActivitySink : ISkipperActivitySink
{
    private int _triggered;

    public void RecordTriggered() => Interlocked.Exchange(ref _triggered, 1);

    public bool Triggered => Volatile.Read(ref _triggered) == 1;
}
