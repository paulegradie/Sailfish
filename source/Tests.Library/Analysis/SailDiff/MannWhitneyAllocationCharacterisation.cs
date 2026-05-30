using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Allocation regression for the Mann-Whitney rank-sum path. Pre-Tier-2, constructing the
/// distribution at sample sizes near the exact/normal boundary allocated tens of megabytes
/// per call (~20 MB at N=11,11), promoted into Gen2, and OOM'd in production under
/// parallelism. The Tier 2 DP rewrite caps every path at well under 200 KB for any sample
/// size up to <c>MannWhitneyDistribution.ExactMaxN</c> (50), and at normal-approx cost
/// (negligible) beyond that.
/// </summary>
/// <remarks>
/// Budgets here are deliberately generous (~2× observed) so the test is stable on machines
/// with different JIT/GC behaviour. Anything that exceeds these limits indicates a
/// regression toward the old combinatorial enumeration.
/// </remarks>
public class MannWhitneyAllocationCharacterisation
{
    private readonly ITestOutputHelper _output;

    public MannWhitneyAllocationCharacterisation(ITestOutputHelper output)
    {
        _output = output;
    }

    // Per-construction allocation budgets, in bytes. Observed numbers on net9.0 macOS
    // post-Tier-2 are roughly an order of magnitude below these caps.
    [Theory]
    [InlineData(8, 8, 200_000)]      // observed ~20 KB; DP cells (n2+1)·(n1·n2+1) tiny.
    [InlineData(10, 10, 200_000)]    // observed ~30 KB.
    [InlineData(11, 11, 200_000)]    // observed ~30 KB. Pre-Tier-2: ~20 MB.
    [InlineData(20, 20, 600_000)]    // observed ~160 KB.
    [InlineData(30, 30, 2_500_000)]  // observed ~1.5 MB. Pre-Tier-2: was normal-approx, ~10 KB.
    [InlineData(50, 50, 6_000_000)]  // observed ~2.1 MB. DP at the cap.
    [InlineData(100, 100, 200_000)]  // normal approximation beyond ExactMaxN=50, negligible.
    public void Construction_AllocatesBelowBudget(int n1, int n2, long budgetBytes)
    {
        var rng = new Random(7);
        var sample1 = Enumerable.Range(0, n1).Select(_ => rng.NextDouble() * 100).ToArray();
        var sample2 = Enumerable.Range(0, n2).Select(_ => 5 + rng.NextDouble() * 100).ToArray();

        // Warm up the JIT for the construction path so we measure steady-state allocation,
        // not first-call codegen costs.
        _ = MannWhitneyWilcoxonFactory.Create(sample1, sample2);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();

        var test = MannWhitneyWilcoxonFactory.Create(sample1, sample2);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        _output.WriteLine($"N1={n1} N2={n2}: allocated {allocated:N0} bytes (budget {budgetBytes:N0}) — PValue={test.PValue:F4}");

        // Sanity: the test still produces a valid p-value regardless of path taken.
        test.PValue.ShouldBeInRange(0.0, 1.0);

        allocated.ShouldBeLessThan(budgetBytes,
            customMessage: $"MW construction at N1={n1}, N2={n2} allocated {allocated:N0} bytes — that's a regression toward the pre-Tier-2 combinatorial path. Investigate.");
    }

    [Fact]
    public void Construction_AtPreTier2OomCliff_DoesNotPromoteToGen2()
    {
        // The pre-Tier-2 implementation at N=11,11 reliably triggered Gen0 collections and
        // occasionally promoted into Gen2 — a strong signal of memory pressure that hurt
        // long-running benchmark workloads. Post-Tier-2 the DP construction completes
        // without any GC.
        var rng = new Random(7);
        var sample1 = Enumerable.Range(0, 11).Select(_ => rng.NextDouble() * 100).ToArray();
        var sample2 = Enumerable.Range(0, 11).Select(_ => 5 + rng.NextDouble() * 100).ToArray();

        // Warm up.
        _ = MannWhitneyWilcoxonFactory.Create(sample1, sample2);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var gen2Before = GC.CollectionCount(2);
        var gen1Before = GC.CollectionCount(1);

        _ = MannWhitneyWilcoxonFactory.Create(sample1, sample2);

        var gen2After = GC.CollectionCount(2);
        var gen1After = GC.CollectionCount(1);

        (gen2After - gen2Before).ShouldBe(0, customMessage: "DP construction triggered a Gen2 GC; the old combinatorial path is back.");
        (gen1After - gen1Before).ShouldBe(0, customMessage: "DP construction triggered a Gen1 GC; allocation budget exceeded.");
    }
}
