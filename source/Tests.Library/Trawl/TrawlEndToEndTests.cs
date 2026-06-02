using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Attributes;
using Shouldly;
using Tests.Common.Utils;
using Tests.E2E.TestSuite;
using Xunit;

namespace Tests.Library.Trawl;

// A Trawl-only [Sailfish] class (no [SailfishMethod]) — proves discovery, validation, routing, and
// result reporting all accept load scenarios end-to-end through the programmatic runner.
[Sailfish]
public class TrawlSmokeScenario
{
    [Trawl(VirtualUsers = 2, DurationSeconds = 0.3, WarmupSeconds = 0)]
    public async Task Ping(CancellationToken ct) => await Task.Delay(2, ct);
}

public class TrawlEndToEndTests
{
    [Fact]
    public async Task TrawlScenario_RunsThroughTheRunner_AndReportsACase()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(TrawlSmokeScenario))
            .WithTestNames(typeof(TrawlSmokeScenario).FullName!)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeTrue();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(0);
        result.ExecutionSummaries.Count().ShouldBe(1);

        var caseResult = result.ExecutionSummaries.Single().CompiledTestCaseResults.Single();
        caseResult.PerformanceRunResult.ShouldNotBeNull();
        caseResult.PerformanceRunResult!.RawExecutionResults.Length.ShouldBeGreaterThan(0);
    }
}
