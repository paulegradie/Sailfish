using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.Common.Utils;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Tests.Library.E2EScenarios.ExceptionCases;

public class WhenOneTestCaseThrows
{
    [Fact]
    public async Task TheRemainingTestCasesStillExecute()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .WithTestNames(nameof(TestsRunIfException))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();
        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();

        result.Exceptions.ShouldNotBeNull();
        result.Exceptions?.Count().ShouldBe(1);

        result.ExecutionSummaries.First().CompiledTestCaseResults.First().TestCaseId.ShouldNotBeNull();
        result.ExecutionSummaries
            .SelectMany(x => x.CompiledTestCaseResults.Select(c => c.PerformanceRunResult))
            .Count(x => x is null)
            .ShouldBe(1);

        result.ExecutionSummaries
            .SelectMany(x => x.CompiledTestCaseResults.Select(c => c.PerformanceRunResult))
            .Count(x => x is not null)
            .ShouldBe(2);
    }
}