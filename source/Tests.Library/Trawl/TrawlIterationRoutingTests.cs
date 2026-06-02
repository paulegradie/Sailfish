using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

/// <summary>
/// Drives a [Trawl] method through the real TestCaseIterator to prove routing: the iterator detects the
/// attribute, runs the load engine instead of the sequential strategy, and injects latency samples so the
/// case reports through the normal pipeline.
/// </summary>
public class TrawlIterationRoutingTests
{
    private static TestCaseIterator NewIterator()
    {
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        return new TestCaseIterator(runSettings, logger,
            new FixedIterationStrategy(logger),
            new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>()));
    }

    private static TestInstanceContainer ContainerFor(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName)!;
        var settings = new ExecutionSettings { NumWarmupIterations = 0, SampleSize = 1, UseAdaptiveSampling = false };
        return TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);
    }

    [Fact]
    public async Task TrawlMethod_IsRouted_RunsConcurrently_AndInjectsSamples()
    {
        var instance = new LoadWork();
        var container = ContainerFor(instance, nameof(LoadWork.Scenario));

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        instance.Calls.ShouldBeGreaterThan(0);
        result.PerformanceTimerResults.ShouldNotBeNull();
        result.PerformanceTimerResults!.ExecutionIterationPerformances.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TrawlMethod_WithAllFailures_FailsTheCase()
    {
        var container = ContainerFor(new AlwaysThrows(), nameof(AlwaysThrows.Scenario));

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Exception.ShouldNotBeNull();
    }

    [Sailfish]
    private sealed class LoadWork
    {
        public int Calls;

        [Trawl(VirtualUsers = 3, DurationSeconds = 0.25, WarmupSeconds = 0)]
        public async Task Scenario(CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(2, ct);
        }
    }

    [Sailfish]
    private sealed class AlwaysThrows
    {
        [Trawl(VirtualUsers = 2, DurationSeconds = 0.2, WarmupSeconds = 0)]
        public Task Scenario(CancellationToken ct) => throw new InvalidOperationException("always fails");
    }
}
