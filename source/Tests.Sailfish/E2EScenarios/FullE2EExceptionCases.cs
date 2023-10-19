using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Test.E2EScenarios;

public class FullE2EExceptionCases
{
    [Fact]
    public async Task GlobalSetupExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(GlobalSetupExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Global Setup Exception");
    }

    [Fact]
    public async Task MethodSetupExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(MethodSetupExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Method Setup Exception");
    }

    [Fact]
    public async Task IterationSetupExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(IterationSetupExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Iteration Setup Exception");
    }

    [Fact]
    public async Task IterationTeardownExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(IterationTeardownExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Iteration Teardown Exception");
        ;
    }

    [Fact]
    public async Task MethodTeardownExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(MethodTeardownExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Method Teardown Exception");
    }

    [Fact]
    public async Task GlobalTeardownExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(GlobalTeardownExceptionIsHandled))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Global Teardown Exception");
    }

    [Fact]
    public async Task MultipleLifecycleExceptionsAreHandledWithMethodTeardownSurfacing()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(MethodTeardownExceptionComesFirst))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Method Teardown Exception");
    }

    [Fact]
    public async Task MultipleLifecycleExceptionsAreHandledWithIterationSetupSurfacing()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(IterationSetupExceptionComesFirst))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(2);

        result.Exceptions.ToList()[0].Message.ShouldBe("Iteration Setup Exception");
        result.Exceptions.ToList()[1].Message.ShouldBe("Iteration Setup Exception");
    }

    [Fact]
    public async Task OnlySailfishMethodThrows()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(OnlyTheSailfishMethodThrows))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Sailfish Method Exception");
    }

    [Fact]
    public async Task VoidMethodRequestsCancellationTokenThrows()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(VoidMethodRequestsCancellationToken))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Parameter injection is not supported for void methods");
    }

    [Fact]
    public async Task MultipleInjectionsOnAMethodThrows()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(MultipleInjectionsOnAsyncMethod))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Parameter injection is only supported for a single parameter which must be a the CancellationToken type");
    }

    [Fact]
    public async Task WhenTestExceptionOccursHandlersAreOnlyGivenRealData()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(SailfishMethodException))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();
        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();

        result.Exceptions.ShouldNotBeNull();
        result.Exceptions?.Count().ShouldBe(1);
        result.ExecutionSummaries
            .SelectMany(x => x.CompiledTestCaseResults.Select(x => x.PerformanceRunResult))
            .Count(x => x is null)
            .ShouldBe(1);
    }
}