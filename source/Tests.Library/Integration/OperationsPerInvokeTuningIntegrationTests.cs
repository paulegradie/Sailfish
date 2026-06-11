using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Execution;
using Sailfish.Execution.Tuning;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Integration;

/// <summary>
/// Exercises OperationsPerInvoke auto-tuning end-to-end through TestCaseIterator, with the tuner's pilot
/// measurements supplied by a scripted <see cref="IOperationsPerInvokeTimer"/> rather than the wall clock.
/// The previous version timed a real ~15ms busy-wait and asserted the tuner chose OPI ≥ 2; under CI/parallel
/// load the pilot stretched until a single invocation looked like it already filled the 45ms target, the
/// tuner returned OPI 1, and the test flaked. The scripted timer still drives the real invocation path (the
/// method under test is genuinely called for every pilot operation), but the durations the tuner reasons
/// over are deterministic — so the tuning DECISION can be asserted exactly.
/// </summary>
public class OperationsPerInvokeTuningIntegrationTests
{
    [Fact]
    public async Task AutoTunesOPI_FastOperation_TunesUpToTargetMultiple()
    {
        // ~15ms/op against a 45ms target ⇒ exactly 3 operations per iteration.
        var opi = await TuneOpiAsync(scriptedPerOpMs: 15.0, targetMs: 45.0);
        opi.ShouldBe(3);
    }

    [Fact]
    public async Task AutoTunesOPI_SubMeasurableOperation_GrowsBatchThenTunes()
    {
        // ~0.1ms/op is below the 2ms measurable floor, so the tuner must grow the pilot batch
        // geometrically (1 → 4 → 16 → 64 ⇒ 6.4ms) before it can estimate per-op time, then scale to
        // the 10ms target ⇒ 100 operations. This locks in the batch-growth path that batching exists
        // for — the sub-microsecond case that previously made the tuner give up at OPI 1.
        var opi = await TuneOpiAsync(scriptedPerOpMs: 0.1, targetMs: 10.0);
        opi.ShouldBe(100);
    }

    [Fact]
    public async Task AutoTunesOPI_SlowOperation_StaysAtOne()
    {
        // A single ~60ms op already overshoots the 45ms target, so batching can't help — OPI must
        // stay 1. This is the counterpart guarantee that wall-clock timing could never assert
        // (it was indistinguishable from the load-induced false negative the flake produced).
        var opi = await TuneOpiAsync(scriptedPerOpMs: 60.0, targetMs: 45.0);
        opi.ShouldBe(1);
    }

    [Fact]
    public async Task OperationsPerInvoke_RecordsPerOperationTime_NotAggregate()
    {
        // Fixed OPI (no tuner): batching N ops must record per-op time, not the N× aggregate. This is
        // the recording-normalization contract; it deliberately uses real timing with loose bounds and
        // is independent of the tuning decision above.
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

    /// <summary>
    /// Runs the full Iterate path with the OPI tuner fed a scripted per-op duration, and returns the OPI
    /// the iterator settled on. The method under test is an instant counter (timing is supplied by the
    /// scripted timer), so the decision is purely a function of scriptedPerOpMs and targetMs.
    /// </summary>
    private static async Task<int> TuneOpiAsync(double scriptedPerOpMs, double targetMs)
    {
        var logger = Substitute.For<ILogger>();
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        var iterator = new TestCaseIterator(
            runSettings, logger,
            new FixedIterationStrategy(logger),
            new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>()))
        {
            OpiTimer = new ScriptedOperationsPerInvokeTimer(scriptedPerOpMs)
        };

        var instance = new CountingWork();
        var method = typeof(CountingWork).GetMethod(nameof(CountingWork.Run))!;
        var settings = new ExecutionSettings
        {
            NumWarmupIterations = 1,
            SampleSize = 5,
            OperationsPerInvoke = 1, // tuner only engages when starting from <= 1
            TargetIterationDuration = TimeSpan.FromMilliseconds(targetMs),
            UseAdaptiveSampling = false
        };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var result = await iterator.Iterate(container, disableOverheadEstimation: true, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();

        return container.ExecutionSettings.OperationsPerInvoke;
    }

    /// <summary>
    /// Drives the real invocation (so the tuner's invocation path is genuinely exercised) but reports a
    /// deterministic batch duration of operations × per-op, decoupling the tuning decision from host load.
    /// </summary>
    private sealed class ScriptedOperationsPerInvokeTimer : IOperationsPerInvokeTimer
    {
        private readonly double _perOpMs;

        public ScriptedOperationsPerInvokeTimer(double perOpMs) => _perOpMs = perOpMs;

        public async Task<double> TimeBatchAsync(Func<Task> invocation, int operations)
        {
            for (var i = 0; i < operations; i++)
                await invocation().ConfigureAwait(false);
            return operations * _perOpMs;
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
