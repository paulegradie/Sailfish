using System;
using System.ComponentModel.DataAnnotations;
using Sailfish.Trawl;

namespace Sailfish.Attributes;

/// <summary>
///     Marks a method as a Trawl <b>load-testing scenario</b>. Instead of being micro-benchmarked
///     sequentially (as with <see cref="SailfishMethodAttribute" />), the method is invoked
///     <i>concurrently</i> by many virtual users for a sustained duration, and Sailfish reports throughput,
///     latency percentiles, and error rate.
/// </summary>
/// <remarks>
///     A method is either a microbenchmark (<see cref="SailfishMethodAttribute" />) or a load scenario
///     (<see cref="TrawlAttribute" />), never both — the <c>SF1022</c> analyzer enforces this. The enclosing
///     class is a normal <c>[Sailfish]</c> class, so the usual lifecycle hooks apply: warm a shared
///     <c>HttpClient</c> or seed data in <c>[SailfishGlobalSetup]</c>. Because all virtual users share the one
///     test instance, scenario state must be thread-safe.
/// </remarks>
/// <seealso cref="SailfishAttribute" />
/// <seealso cref="LoadModel" />
[AttributeUsage(AttributeTargets.Method)]
public sealed class TrawlAttribute : Attribute
{
    public TrawlAttribute()
    {
    }

    /// <summary>Number of concurrent virtual users (closed model). Default 10.</summary>
    [Range(1, int.MaxValue)]
    public int VirtualUsers { get; set; } = 10;

    /// <summary>Sustained, measured load duration in seconds (after warmup). Default 30.</summary>
    public double DurationSeconds { get; set; } = 30;

    /// <summary>Warmup duration in seconds; traffic is generated but not measured. Default 5.</summary>
    public double WarmupSeconds { get; set; } = 5;

    /// <summary>The load model to apply. Default <see cref="LoadModel.ClosedModel" />.</summary>
    public LoadModel Model { get; set; } = LoadModel.ClosedModel;

    /// <summary>
    ///     Target arrival rate in requests/second for <see cref="LoadModel.OpenModel" />. Ignored by the
    ///     closed model. 0 (default) means unset.
    /// </summary>
    public double TargetRequestsPerSecond { get; set; }

    /// <summary>When <c>true</c>, this load scenario is skipped. Default <c>false</c>.</summary>
    public bool Disabled { get; set; }
}
