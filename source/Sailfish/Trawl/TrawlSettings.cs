namespace Sailfish.Trawl;

/// <summary>
///     Run-wide knobs and overrides for Trawl (load testing) scenarios. These are distinct from the
///     per-scenario <see cref="Sailfish.Attributes.TrawlAttribute" />: the attribute authors the scenario,
///     while these settings let you reshape every scenario at run time — most usefully to shrink a load run
///     in CI (fewer virtual users, a capped duration) without editing the test source.
///     <para>
///         Every override is <c>null</c> / <c>false</c> by default, meaning "use the per-scenario attribute
///         value". A non-null override replaces the attribute value for every scenario in the run.
///     </para>
/// </summary>
public class TrawlSettings
{
    /// <summary>
    ///     Global kill switch. When <c>true</c>, every <c>[Trawl]</c> load scenario is skipped while the
    ///     rest of the run proceeds normally. Default <c>false</c>.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    ///     Overrides the number of concurrent virtual users for every closed-model scenario. <c>null</c>
    ///     (default) leaves each scenario's own <see cref="Sailfish.Attributes.TrawlAttribute.VirtualUsers" />
    ///     in effect.
    /// </summary>
    public int? VirtualUsersOverride { get; set; }

    /// <summary>
    ///     Caps the sustained (measured) load duration in seconds for every scenario. A scenario whose
    ///     attribute requests a longer duration is clamped to this value. <c>null</c> (default) means no cap.
    /// </summary>
    public double? MaxDurationSecondsOverride { get; set; }

    /// <summary>
    ///     Overrides the warmup duration in seconds for every scenario. <c>null</c> (default) leaves each
    ///     scenario's own warmup in effect.
    /// </summary>
    public double? WarmupSecondsOverride { get; set; }

    /// <summary>A fresh instance with all-default (no-override) values.</summary>
    public static TrawlSettings Default => new();
}
