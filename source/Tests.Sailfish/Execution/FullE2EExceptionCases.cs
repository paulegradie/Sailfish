using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Test.Execution;

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

        result.IsValid.ShouldBe(false);
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Multiple methods with attribute SailfishIterationSetupAttribute found");
    }
}