using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.E2ETestSuite;
using Tests.E2ETestSuite.Discoverable;
using Xunit;

namespace Test.E2EScenarios;

public class FullE2EFixture
{
    [Fact]
    public async Task AFullTestRunOfTheDemoDoesNotError()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(0);
    }

    // will need to update this if more tests are added to the the project
    [Fact]
    public async Task AFullTestRunOfTheDemoShouldFind13Tests()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.ExecutionSummaries.Count().ShouldBe(13);
    }

    [Fact]
    public async Task GlobalSampleSizeOverrideIsApplied()
    {
        const int sampleSizeOverride = 4;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(nameof(MinimalTest))
            .DisableOverheadEstimation()
            .WithGlobalSampleSize(sampleSizeOverride)
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.ExecutionSummaries.Count().ShouldBe(1);
        result.ExecutionSummaries.Single().CompiledTestCaseResults.Count().ShouldBe(1);
        result.ExecutionSummaries.Single().CompiledTestCaseResults.Single().PerformanceRunResult.ShouldNotBeNull();
        result.ExecutionSummaries.Single().CompiledTestCaseResults.Single().PerformanceRunResult?.RawExecutionResults.Length.ShouldBe(sampleSizeOverride);
    }
}