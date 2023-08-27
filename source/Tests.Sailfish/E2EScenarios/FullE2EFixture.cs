using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Shouldly;
using Tests.E2ETestSuite;
using Xunit;

namespace Test.E2EScenarios;

public class FullE2EFixture
{
    [Fact]
    public async Task AFullTestRunOfTheDemoDoesNotError()
    {
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestRegistrationProvider))
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
            .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesFromAnchorTypes(typeof(E2ETestRegistrationProvider))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.ExecutionSummaries.Count().ShouldBe(13);
    }
}