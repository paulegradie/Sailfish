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
    public async Task TestCaseIterator_AutoTunesOPI_MedianNearTarget()
    {
        // Arrange: per-op ~15ms (busy-wait), target ~45ms should yield OPI >= 2
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        var fixedStrategy = new FixedIterationStrategy(logger);
        var adaptiveStrategy = new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>());
        var iterator = new TestCaseIterator(runSettings, logger, fixedStrategy, adaptiveStrategy);

        var instance = new DelayWork(15);
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
        container.ExecutionSettings.OperationsPerInvoke.ShouldBeGreaterThanOrEqualTo(2);

        var durations = container.CoreInvoker.GetPerformanceResults().ExecutionIterationPerformances
            .Select(p => p.GetDurationFromTicks().MilliSeconds.Duration)
            .OrderBy(x => x)
            .ToArray();
        durations.Length.ShouldBeGreaterThan(0);
        var median = durations[durations.Length / 2];

        var target = settings.TargetIterationDuration.TotalMilliseconds;
        // Loose bounds to account for timer granularity on CI windows VMs
        median.ShouldBeGreaterThan(target * 0.6);
        median.ShouldBeLessThan(target * 1.6);
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

