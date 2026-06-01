using System.Collections.Generic;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     A concise, reliability-focused snapshot of the machine and runtime the benchmark ran on, projected from
///     Sailfish's reproducibility manifest and environment health check. Lets Skipper describe the environment
///     and — crucially — temper its verdict when the host is noisy or misconfigured (the dominant failure mode of
///     microbenchmarking). Volatile fields (timestamp, session id) are deliberately excluded so the context hash —
///     and therefore the cached narrative — stays stable across runs on the same machine and commit.
/// </summary>
public sealed record EnvironmentSnapshot(
    string DotNetRuntime,
    string Os,
    string OsArchitecture,
    string ProcessArchitecture,
    string? CpuModel,
    string GcMode,
    string Jit,
    string CpuAffinity,
    string Timer,
    int HealthScore,
    string? HealthLabel,
    string? CiSystem,
    string? CommitSha,
    IReadOnlyList<EnvironmentConcern> Concerns);

/// <summary>A single environment health concern (a Warn or Fail entry) the agent should weigh when judging reliability.</summary>
public sealed record EnvironmentConcern(
    string Name,
    string Status,
    string Details,
    string? Recommendation);
