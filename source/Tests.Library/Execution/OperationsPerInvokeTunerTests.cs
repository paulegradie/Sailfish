using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;
using System.Reflection;


namespace Tests.Library.Execution;

public class OperationsPerInvokeTunerTests
{
    private readonly ILogger logger = Substitute.For<ILogger>();

    [Fact]
    public async Task Tune_FastRelativeToTarget_IncreasesOPI()
    {
        // Arrange: per-op ~10ms, target 30ms -> expect OPI >= 2
        var instance = new DelayWork(10);
        var method = typeof(DelayWork).GetMethod(nameof(DelayWork.Run))!;
        var settings = new ExecutionSettings { OperationsPerInvoke = 1, NumWarmupIterations = 0, SampleSize = 3 };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var tuner = new OperationsPerInvokeTuner();

        // Act
        var tuned = await tuner.TuneAsync(container, TimeSpan.FromMilliseconds(30), logger, CancellationToken.None);

        // Assert
        tuned.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Tune_TargetLessThanPerOp_ReturnsOne()
    {
        // Arrange: per-op ~25ms, target 10ms -> expect OPI == 1
        var instance = new DelayWork(25);
        var method = typeof(DelayWork).GetMethod(nameof(DelayWork.Run))!;
        var settings = new ExecutionSettings { OperationsPerInvoke = 1, NumWarmupIterations = 0, SampleSize = 3 };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var tuner = new OperationsPerInvokeTuner();

        // Act
        var tuned = await tuner.TuneAsync(container, TimeSpan.FromMilliseconds(10), logger, CancellationToken.None);

        // Assert
        tuned.ShouldBe(1);
    }

    [Fact]
    public async Task Tune_DisabledByZeroTarget_ReturnsExistingOPI()
    {
        // Arrange
        var instance = new DelayWork(5);
        var method = typeof(DelayWork).GetMethod(nameof(DelayWork.Run))!;
        var settings = new ExecutionSettings { OperationsPerInvoke = 7, NumWarmupIterations = 0, SampleSize = 3 };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var tuner = new OperationsPerInvokeTuner();

        // Act
        var tuned = await tuner.TuneAsync(container, TimeSpan.Zero, logger, CancellationToken.None);

        // Assert
        tuned.ShouldBe(7);
    }

    private sealed class DelayWork
    {
        private readonly int ms;
        public DelayWork(int ms) => this.ms = ms;
        public Task Run(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < ms)
            {
                if (ct.IsCancellationRequested) break;
                System.Threading.Thread.SpinWait(1000);
            }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverage()
    {
        var values = new System.Collections.Generic.List<double> { 1.0, 3.0 };
        var mi = typeof(OperationsPerInvokeTuner).GetMethod("Median", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (double)mi.Invoke(null, new object[] { values })!;
        result.ShouldBe(2.0, 1e-9);
    }

    [Fact]
    public void Median_Empty_ReturnsZero()
    {
        var values = new System.Collections.Generic.List<double>();
        var mi = typeof(OperationsPerInvokeTuner).GetMethod("Median", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (double)mi.Invoke(null, new object[] { values })!;
        result.ShouldBe(0.0, 1e-9);
    }

}

