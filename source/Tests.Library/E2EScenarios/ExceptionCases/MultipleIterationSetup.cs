using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.Common.Utils;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Tests.Library.E2EScenarios.ExceptionCases;

public class MultipleIterationSetup
{
    [Fact]
    public async Task MultipleLifecycleExceptionsAreHandledWithIterationSetupSurfacing()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .WithTestNames(nameof(IterationSetupExceptionComesFirst))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.ExecutionSummaries.First().CompiledTestCaseResults.First().TestCaseId.ShouldNotBeNull();
        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(2);
        result.Exceptions.ToList()[0].Message.ShouldBe("Iteration Setup Exception");
        result.Exceptions.ToList()[1].Message.ShouldBe("Iteration Setup Exception");
    }
}