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

/// <summary>
/// Exercises the steady-state warmup loop (floor / window / early-stop / cap) end-to-end through
/// TestCaseIterator, with the warmup durations supplied by a scripted <see cref="ISteadyStateWarmupTimer"/>
/// rather than the wall clock. The previous version timed a real ~3ms spin-wait, which made "a stable
/// method stops early" depend on host load: under CI/parallel-suite pressure the spin stretched, the
/// detector's CV threshold blew out, and the test flaked. The scripted timer still drives the real
/// invocation path (the method under test is genuinely called for every warmup), but the durations fed
/// to the detector are deterministic.
/// </summary>
public class SteadyStateWarmupIntegrationTests
{
    private const int Window = SteadyStateWarmupDetector.DefaultWindow; // detector window (effective minimum before a decision)

    private static TestCaseIterator NewIterator(ISteadyStateWarmupTimer warmupTimer)
    {
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        return new TestCaseIterator(runSettings, logger,
            new FixedIterationStrategy(logger),
            new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>()))
        {
            WarmupTimer = warmupTimer
        };
    }

    [Fact]
    public async Task SteadyStateWarmup_StableMethod_StopsEarly()
    {
        const int sampleSize = 2;
        const int maxWarmup = 50;
        var instance = new CountingWork();
        var method = typeof(CountingWork).GetMethod(nameof(CountingWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = 2, // floor below the window — the window governs the minimum
            SampleSize = sampleSize,
            UseSteadyStateWarmup = true,
            MaxWarmupIterations = maxWarmup,
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        // Perfectly stable durations: zero drift, zero CV — steady at the first possible decision.
        var result = await NewIterator(new ScriptedWarmupTimer(_ => 3.0))
            .Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var warmups = instance.Calls - sampleSize; // total invocations minus measured samples
        warmups.ShouldBe(Window, "a perfectly stable signal must be declared steady at the first decision point");
        warmups.ShouldBeLessThan(maxWarmup); // stopped early — did not hit the cap
    }

    [Fact]
    public async Task SteadyStateWarmup_RespectsFloor()
    {
        const int sampleSize = 2;
        const int floor = 12; // floor > window, so the floor governs the minimum
        var instance = new CountingWork();
        var method = typeof(CountingWork).GetMethod(nameof(CountingWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = floor,
            SampleSize = sampleSize,
            UseSteadyStateWarmup = true,
            MaxWarmupIterations = 50,
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var result = await NewIterator(new ScriptedWarmupTimer(_ => 3.0))
            .Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var warmups = instance.Calls - sampleSize;
        // Even a perfectly stable signal must not stop before the configured floor; with stable
        // durations the first permitted decision succeeds, so the floor is also the stopping point.
        warmups.ShouldBe(floor);
    }

    [Fact]
    public async Task SteadyStateWarmup_UnstableMethod_HitsCap()
    {
        // The counterpart guarantee — previously untestable with wall-clock timing: a signal that
        // never stabilises (alternating 3ms / 30ms ⇒ CV far above the detector threshold at every
        // window) must run warmup all the way to the cap, never declaring steady state.
        const int sampleSize = 2;
        const int maxWarmup = 20;
        var instance = new CountingWork();
        var method = typeof(CountingWork).GetMethod(nameof(CountingWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = 2,
            SampleSize = sampleSize,
            UseSteadyStateWarmup = true,
            MaxWarmupIterations = maxWarmup,
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var result = await NewIterator(new ScriptedWarmupTimer(i => i % 2 == 0 ? 3.0 : 30.0))
            .Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var warmups = instance.Calls - sampleSize;
        warmups.ShouldBe(maxWarmup, "an unstable signal must never be declared steady before the cap");
    }

    /// <summary>
    /// Drives the real invocation (so call counting stays honest) but reports scripted durations,
    /// decoupling the warmup decision from host load.
    /// </summary>
    private sealed class ScriptedWarmupTimer : ISteadyStateWarmupTimer
    {
        private readonly Func<int, double> _durationForInvocation;
        private int _invocationIndex;

        public ScriptedWarmupTimer(Func<int, double> durationForInvocation)
        {
            _durationForInvocation = durationForInvocation;
        }

        public async Task<double> TimeAsync(Func<Task> invocation)
        {
            await invocation().ConfigureAwait(false);
            return _durationForInvocation(_invocationIndex++);
        }
    }

    private sealed class CountingWork
    {
        public int Calls;

        public Task Run(CancellationToken ct)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
