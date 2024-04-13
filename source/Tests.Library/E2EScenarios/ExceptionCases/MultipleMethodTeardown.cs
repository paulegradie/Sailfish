using Sailfish;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Tests.Common.Utils;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Tests.Library.E2EScenarios.ExceptionCases;

public class MultipleMethodTeardown
{
    [Fact]
    public async Task MultipleLifecycleExceptionsAreHandledWithMethodTeardownSurfacing()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .WithTestNames(nameof(MethodTeardownExceptionComesFirst))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.ExecutionSummaries.First().CompiledTestCaseResults.First().TestCaseId.ShouldNotBeNull();
        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.Single().Message.ShouldBe("Method Teardown Exception");
    }
}