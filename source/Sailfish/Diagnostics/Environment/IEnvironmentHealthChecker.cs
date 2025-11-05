using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Diagnostics.Environment;

public sealed class EnvironmentHealthCheckContext
{
    // Optional path to a representative test assembly (for build config heuristics)
    public string? TestAssemblyPath { get; init; }
}

public interface IEnvironmentHealthChecker
{
    Task<EnvironmentHealthReport> CheckAsync(EnvironmentHealthCheckContext? context = null, CancellationToken cancellationToken = default);
}

