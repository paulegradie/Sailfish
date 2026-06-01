namespace Sailfish.Trawl;

/// <summary>
///     Selects how load is applied to a <see cref="Sailfish.Attributes.TrawlAttribute" /> scenario.
/// </summary>
public enum LoadModel
{
    /// <summary>
    ///     A fixed number of concurrent virtual users, each looping (run the scenario, then immediately
    ///     run it again) for the scenario duration. Throughput is emergent — it is whatever the system
    ///     under test can sustain at that concurrency. This is the default and the only model honored by
    ///     the initial Trawl execution engine.
    /// </summary>
    ClosedModel = 0,

    /// <summary>
    ///     Requests are dispatched at a fixed target arrival rate (requests/second) regardless of how many
    ///     are already in flight — the "open" model that proper load tests use to expose coordinated
    ///     omission. Honored once the arrival-rate scheduler ships in a later release.
    /// </summary>
    OpenModel = 1
}
