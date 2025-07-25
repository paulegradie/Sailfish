using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.Common.Utils;
using Tests.E2E.TestSuite;
using Tests.E2E.TestSuite.Discoverable;
using Xunit;

namespace Tests.Library.E2EScenarios.SuccessCases;

public class FullRuns
{
    [Fact]
    public async Task GlobalSampleSizeOverrideIsApplied()
    {
        const int sampleSizeOverride = 4;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(typeof(MinimalTest).FullName!)
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

    // will need to update this if more tests are added to the project
    [Fact]
    public async Task AFullTestRunOfTheDemoShouldFindAllTests()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.ExecutionSummaries.Count().ShouldBe(16);
    }

    [Fact]
    public async Task AFullTestRunOfTheDemoDoesNotError()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
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

    [Fact]
    public async Task AlternativeRunCommandWorks()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings, builder => Console.Write(string.Empty), CancellationToken.None);
        result.IsValid.ShouldBe(true);
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count().ShouldBe(0);
    }
}