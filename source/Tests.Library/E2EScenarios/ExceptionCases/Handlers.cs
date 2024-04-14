using Sailfish;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Tests.Common.Utils;
using Tests.E2E.ExceptionHandling;
using Tests.E2E.ExceptionHandling.Tests;
using Xunit;

namespace Tests.Library.E2EScenarios.ExceptionCases;

public class Handlers
{
    [Fact]
    public async Task WhenTestExceptionOccursHandlersAreOnlyGivenRealData()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .WithTestNames(nameof(SailfishMethodException))
            .ProvidersFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestExceptionHandlingProvider))
            .Build();
        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeFalse();

        result.Exceptions.ShouldNotBeNull();
        result.Exceptions?.Count().ShouldBe(1);
        result.ExecutionSummaries
            .SelectMany(x => x.CompiledTestCaseResults.Select(c => c.PerformanceRunResult))
            .Count(x => x is null)
            .ShouldBe(1);
    }
}