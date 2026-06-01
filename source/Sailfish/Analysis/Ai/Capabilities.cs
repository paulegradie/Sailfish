using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Marker for a capability an agent may be granted for a session. Capabilities are the mechanism by which
///     Skipper escalates from "explain" (read-only) to "act" (open PRs, file tickets, query telemetry) without
///     changing the <see cref="ISailfishAgent" /> contract — the host simply grants more of them.
/// </summary>
public interface ISkipperCapability
{
}

/// <summary>Grants read-only access to the repository under <see cref="RepositoryRoot" />. Shipped, and granted locally.</summary>
public interface ICodeReadCapability : ISkipperCapability
{
    string RepositoryRoot { get; }
}

/// <summary>Reserved (not yet wired): query a telemetry backend such as Azure Log Analytics or Databricks.</summary>
public interface ITelemetryQueryCapability : ISkipperCapability
{
}

/// <summary>Reserved (not yet wired): open or update a pull request.</summary>
public interface IVersionControlCapability : ISkipperCapability
{
}

/// <summary>Reserved (not yet wired): file or update a work-tracking ticket.</summary>
public interface ITicketCapability : ISkipperCapability
{
}

/// <summary>Reserved (not yet wired): synthesize and run a benchmark targeting a diff.</summary>
public interface IBenchmarkSynthesisCapability : ISkipperCapability
{
}

/// <summary>The set of capabilities granted to an agent for a session.</summary>
public interface ICapabilityRegistry
{
    /// <summary>All granted capabilities.</summary>
    IReadOnlyCollection<ISkipperCapability> Granted { get; }

    /// <summary>True when a capability assignable to <typeparamref name="TCapability" /> has been granted.</summary>
    bool Has<TCapability>() where TCapability : class, ISkipperCapability;

    /// <summary>The granted capability assignable to <typeparamref name="TCapability" />, or <c>null</c>.</summary>
    TCapability? Get<TCapability>() where TCapability : class, ISkipperCapability;
}

internal sealed class CapabilityRegistry : ICapabilityRegistry
{
    private readonly IReadOnlyCollection<ISkipperCapability> capabilities;

    public CapabilityRegistry(IEnumerable<ISkipperCapability> capabilities)
    {
        this.capabilities = capabilities.ToArray();
    }

    public IReadOnlyCollection<ISkipperCapability> Granted => capabilities;

    public bool Has<TCapability>() where TCapability : class, ISkipperCapability => Get<TCapability>() is not null;

    public TCapability? Get<TCapability>() where TCapability : class, ISkipperCapability =>
        capabilities.OfType<TCapability>().FirstOrDefault();
}

/// <summary>Default read-only code-access capability granted for the local Explain flow.</summary>
internal sealed record CodeReadCapability(string RepositoryRoot) : ICodeReadCapability;
