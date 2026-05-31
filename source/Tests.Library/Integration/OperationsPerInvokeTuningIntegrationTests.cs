using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Integration;

public class OperationsPerInvokeTuningIntegrationTests
{
    [Fact]
    public async Task TestCaseIterator_AutoTunesOPI_RecordsPerOperationTime()
    {
        // Arrange: per-op ~15ms (busy-wait), target ~45ms should yield OPI >= 2
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        var fixedStrategy = new FixedIterationStrategy(logger);
        var adaptiveStrategy = new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>());
        var iterator = new TestCaseIterator(runSettings, logger, fixedStrategy, adaptiveStrategy);

        const double perOpMs = 15.0;
        var instance = new DelayWork((int)perOpMs);
        var method = typeof(DelayWork).GetMethod(nameof(DelayWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = 1,
            SampleSize = 5,
            OperationsPerInvoke = 1,
            TargetIterationDuration = TimeSpan.FromMilliseconds(45),
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        // Act
        var result = await iterator.Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var opi = container.ExecutionSettings.OperationsPerInvoke;
        opi.ShouldBeGreaterThanOrEqualTo(2);

        var durations = container.CoreInvoker.GetPerformanceResults().ExecutionIterationPerformances
            .Select(p => p.GetDurationFromTicks().MilliSeconds.Duration)
            .OrderBy(x => x)
            .ToArray();
        durations.Length.ShouldBeGreaterThan(0);
        var median = durations[durations.Length / 2];

        var target = settings.TargetIterationDuration.TotalMilliseconds;

        // The recorded sample is per-OPERATION (~15ms), NOT the per-ITERATION aggregate (~45ms target).
        // Loose bounds to account for timer granularity on CI VMs.
        median.ShouldBeGreaterThan(perOpMs * 0.5);
        median.ShouldBeLessThan(perOpMs * 2.0); // < 30ms: also proves it is not the inflated ~45ms aggregate

        // The tuned iteration (perOp * OPI) still lands near the target — tuning and normalization agree.
        var iterationMs = median * opi;
        iterationMs.ShouldBeGreaterThan(target * 0.5);
        iterationMs.ShouldBeLessThan(target * 2.0);
    }

    [Fact]
    public async Task OperationsPerInvoke_RecordsPerOperationTime_NotAggregate()
    {
        // Fixed OPI (no tuner): batching N ops must record per-op time, not the N× aggregate.
        const double perOpMs = 10.0;
        const int opi = 4;
        var instance = new DelayWork((int)perOpMs);
        var method = typeof(DelayWork).GetMethod(nameof(DelayWork.Run))!;
        var settings = new ExecutionSettings { NumWarmupIterations = 0, SampleSize = 3, OperationsPerInvoke = opi };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        for (var i = 0; i < 3; i++)
            await container.CoreInvoker.ExecutionMethodWithOperationsPerInvoke(opi, CancellationToken.None);

        var durations = container.CoreInvoker.GetPerformanceResults().ExecutionIterationPerformances
            .Select(p => p.GetDurationFromTicks().MilliSeconds.Duration)
            .OrderBy(x => x)
            .ToArray();
        var median = durations[durations.Length / 2];

        // ~10ms per op, NOT the ~40ms (4×) aggregate.
        median.ShouldBeGreaterThan(perOpMs * 0.5);
        median.ShouldBeLessThan(perOpMs * 2.5); // < 25ms, well below the ~40ms aggregate
    }

    private sealed class DelayWork
    {
        private readonly int _ms;
        public DelayWork(int ms) => _ms = ms;
        public Task Run(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < _ms)
            {
                if (ct.IsCancellationRequested) break;
                Thread.SpinWait(1000);
            }
            return Task.CompletedTask;
        }
    }
}
