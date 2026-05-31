using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Integration;

public class SteadyStateWarmupIntegrationTests
{
    private const int Window = SteadyStateWarmupDetector.DefaultWindow; // detector window (effective minimum before a decision)

    private static TestCaseIterator NewIterator()
    {
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        return new TestCaseIterator(runSettings, logger,
            new FixedIterationStrategy(logger),
            new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>()));
    }

    [Fact]
    public async Task SteadyStateWarmup_StableMethod_StopsEarly()
    {
        const int sampleSize = 2;
        const int maxWarmup = 50;
        var instance = new CountingStableWork(3); // stable ~3ms/call
        var method = typeof(CountingStableWork).GetMethod(nameof(CountingStableWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = 2, // floor
            SampleSize = sampleSize,
            UseSteadyStateWarmup = true,
            MaxWarmupIterations = maxWarmup,
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var warmups = instance.Calls - sampleSize; // total invocations minus measured samples
        warmups.ShouldBeGreaterThanOrEqualTo(Window); // can't decide before the window fills
        warmups.ShouldBeLessThan(maxWarmup);          // stopped early — did not hit the cap
    }

    [Fact]
    public async Task SteadyStateWarmup_RespectsFloor()
    {
        const int sampleSize = 2;
        const int floor = 12; // floor > window, so the floor governs the minimum
        var instance = new CountingStableWork(3);
        var method = typeof(CountingStableWork).GetMethod(nameof(CountingStableWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = floor,
            SampleSize = sampleSize,
            UseSteadyStateWarmup = true,
            MaxWarmupIterations = 50,
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var warmups = instance.Calls - sampleSize;
        warmups.ShouldBeGreaterThanOrEqualTo(floor); // never stops before the configured floor
    }

    private sealed class CountingStableWork
    {
        private readonly int _ms;
        public int Calls;
        public CountingStableWork(int ms) => _ms = ms;

        public Task Run(CancellationToken ct)
        {
            Calls++;
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
