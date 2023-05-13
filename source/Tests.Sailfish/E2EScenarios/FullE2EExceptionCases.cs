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
    public async Task ATestRunWithDuplicateLifecycleMethodsReturnsException()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(ADuplicateLifeCycle))
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Multiple methods with attribute SailfishIterationSetupAttribute found");
    }

    [Fact]
    public async Task GlobalSetupExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(GlobalSetupExceptionIsHandled))
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Iteration Teardown Exception");;
    }

    [Fact]
    public async Task MethodTeardownExceptionsAreHandled()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(MethodTeardownExceptionIsHandled))
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Sailfish Method Exception");
    }
}