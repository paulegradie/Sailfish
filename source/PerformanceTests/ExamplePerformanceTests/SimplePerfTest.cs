﻿using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2, Disabled = TestConstants.Disabled)]
public class SimplePerfTest
{
    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
    }
}