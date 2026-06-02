namespace Sailfish.Trawl;

/// <summary>
///     Selects how load is applied to a <see cref="Sailfish.Attributes.TrawlAttribute" /> scenario.
/// </summary>
public enum LoadModel
{
    /// <summary>
    ///     A fixed number of concurrent virtual users, each looping (run the scenario, then immediately
    ///     run it again) for the scenario duration. Throughput is emergent — it is whatever the system
    ///     under test can sustain at that concurrency. This is the default model.
    /// </summary>
    ClosedModel = 0,

    /// <summary>
    ///     Requests are dispatched at a fixed target arrival rate (requests/second) regardless of how many
    ///     are already in flight — the "open" model that proper load tests use to expose a system that
    ///     can't keep up. Latency is measured from each request's <i>intended</i> send time
    ///     (coordinated-omission correction), and <see cref="Sailfish.Attributes.TrawlAttribute.VirtualUsers" />
    ///     caps the number of concurrent in-flight requests (think connection-pool size).
    /// </summary>
    OpenModel = 1
}
