using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Groups tests that load and walk this project's compiled DLL through
/// <c>TestDiscovery</c> / <c>DllFinder</c>. Running them in parallel across
/// classes occasionally produces empty discovery results — the failure mode
/// looks like a discovery-time race against the build directory rather than a
/// real bug, so we serialize the fixtures instead of paying the cost of
/// reproducing it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AssemblyDiscoveryCollection
{
    public const string Name = "AssemblyDiscovery";
}
