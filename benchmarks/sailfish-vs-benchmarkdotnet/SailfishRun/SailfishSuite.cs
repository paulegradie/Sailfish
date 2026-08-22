using Sailfish.Attributes;

namespace ToolCompare;

// 10,000 measured samples per method + 10 warmups: enough mass for stable p99/p99.9 estimates.
[Sailfish(SampleSize = 10000, NumWarmupIterations = 10, DisableOverheadEstimation = false)]
public class SailfishSuite
{
    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _ = EfFixture.Instance; // build + seed the db once, outside measurement
        Workloads.TinyOp();
        Workloads.CpuHash();
        Workloads.EfCoreQuery();
    }

    [SailfishMethod]
    public void TinyOp() => Workloads.TinyOp();

    [SailfishMethod]
    public void CpuHash() => Workloads.CpuHash();

    [SailfishMethod]
    public void EfCoreQuery() => Workloads.EfCoreQuery();
}
