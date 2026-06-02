using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Trawl;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

/// <summary>
/// Routes [Trawl(Model = OpenModel)] scenarios through the real TestCaseIterator to prove the engine
/// dispatches them via the arrival-rate scheduler and validates the open-model configuration.
/// </summary>
public class TrawlOpenModelRoutingTests
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
    public async Task OpenModelScenario_IsRouted_ProducesSamples()
    {
        var instance = new OpenModelWork();
        var container = ContainerFor(instance, nameof(OpenModelWork.Scenario));

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        instance.Calls.ShouldBeGreaterThan(0);
        result.PerformanceTimerResults!.ExecutionIterationPerformances.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task OpenModel_WithoutTargetRate_FailsWithHelpfulMessage()
    {
        var container = ContainerFor(new MisconfiguredOpenModel(), nameof(MisconfiguredOpenModel.Scenario));

        var result = await NewIterator().Iterate(container, disableOverheadEstimation: true, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Exception.ShouldNotBeNull();
        result.Exception!.Message.ShouldContain("TargetRequestsPerSecond");
    }

    [Sailfish]
    private sealed class OpenModelWork
    {
        public int Calls;

        [Trawl(Model = LoadModel.OpenModel, TargetRequestsPerSecond = 100, DurationSeconds = 0.3, WarmupSeconds = 0, VirtualUsers = 8)]
        public async Task Scenario(CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(2, ct);
        }
    }

    [Sailfish]
    private sealed class MisconfiguredOpenModel
    {
        // OpenModel but no TargetRequestsPerSecond — should fail fast with a clear message.
        [Trawl(Model = LoadModel.OpenModel, DurationSeconds = 0.2, WarmupSeconds = 0)]
        public Task Scenario(CancellationToken ct) => Task.CompletedTask;
    }
}
