using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;

namespace Tests.Library.Execution;

public class TestCaseIteratorDecouplingTests
{
    private class Dummy
    {
        public void Run() { }
    }

    private static TestInstanceContainer CreateContainer(bool useAdaptive, int sampleSize = 1, int warmups = 0)
    {
        var instance = new Dummy();
        var method = typeof(Dummy).GetMethod(nameof(Dummy.Run))!;
        var exec = Substitute.For<IExecutionSettings>();
        exec.UseAdaptiveSampling.Returns(useAdaptive);
        exec.SampleSize.Returns(sampleSize);
        exec.NumWarmupIterations.Returns(warmups);
        exec.MinimumSampleSize.Returns(1);
        exec.MaximumSampleSize.Returns(sampleSize);

        return TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), disabled: false, exec);
    }

    [Fact]
    public async Task AdaptiveSampling_StillUsed_WhenOverheadDisabled()
    {
        var runSettings = Substitute.For<IRunSettings>();
        var logger = Substitute.For<ILogger>();

        var fixedStrategy = Substitute.For<IIterationStrategy>();
        fixedStrategy.ExecuteIterations(Arg.Any<TestInstanceContainer>(), Arg.Any<IExecutionSettings>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IterationResult { IsSuccess = true, TotalIterations = 1, ConvergedEarly = false }));

        var adaptiveStrategy = Substitute.For<IIterationStrategy>();
        adaptiveStrategy.ExecuteIterations(Arg.Any<TestInstanceContainer>(), Arg.Any<IExecutionSettings>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IterationResult { IsSuccess = true, TotalIterations = 1, ConvergedEarly = true, ConvergenceReason = "test" }));

        var iterator = new TestCaseIterator(runSettings, logger, fixedStrategy, adaptiveStrategy);
        var container = CreateContainer(useAdaptive: true);

        var result = await iterator.Iterate(container, disableOverheadEstimation: true, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();

        await adaptiveStrategy.Received(1).ExecuteIterations(Arg.Any<TestInstanceContainer>(), Arg.Any<IExecutionSettings>(), Arg.Any<CancellationToken>());
        await fixedStrategy.DidNotReceive().ExecuteIterations(Arg.Any<TestInstanceContainer>(), Arg.Any<IExecutionSettings>(), Arg.Any<CancellationToken>());
    }
}

