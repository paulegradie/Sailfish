using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;

namespace ToolCompare;

// Two jobs:
//  - BDN-Default: BenchmarkDotNet's stock methodology (pilot stage picks an invocation count so each
//    iteration runs ~250ms of ops; each reported measurement is a MEAN over that batch).
//  - BDN-PerInvocation: 1 op per iteration, 100 iterations — the same "one sample = one invocation"
//    shape Sailfish uses, so the violins compare like-for-like.
public class BdnConfig : ManualConfig
{
    public BdnConfig()
    {
        AddJob(Job.Default.WithId("BDN-Default"));
        AddJob(Job.Default
            .WithWarmupCount(10)
            .WithIterationCount(10000)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithId("BDN-PerInvocation"));
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(BdnConfig))]
public class BdnSuite
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        _ = EfFixture.Instance;
        Workloads.TinyOp();
        Workloads.CpuHash();
        Workloads.EfCoreQuery();
    }

    [Benchmark]
    public int TinyOp() => Workloads.TinyOp();

    [Benchmark]
    public int CpuHash() => Workloads.CpuHash();

    [Benchmark]
    public int EfCoreQuery() => Workloads.EfCoreQuery();
}
