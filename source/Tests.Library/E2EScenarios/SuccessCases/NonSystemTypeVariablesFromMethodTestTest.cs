using Sailfish;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Common.Utils;
using Tests.E2E.TestSuite;
using Tests.E2E.TestSuite.Discoverable;
using Xunit;

namespace Tests.Library.E2EScenarios.SuccessCases;

public class NonSystemTypeVariablesFromMethodTestTest
{
    [Fact]
    public async Task NonSystemVariablesCanBeSuppliedFromAMethod()
    {
        // Will hold the variables the perf tests where executed with
        var testVariables = new List<MyNonSystemType>();
        NonSystemTypeVariablesFromMethodTest.CaptureStringVariablesForTestingThisTest.Value = testVariables;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(nameof(NonSystemTypeVariablesFromMethodTest))
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBe(true);
        result.ExecutionSummaries.Count().ShouldBe(1);


        // The variables should be processed in their natural order
        // We have each variable twice since we have warm-ups
        testVariables.ShouldBeEquivalentTo(new List<MyNonSystemType>
        {
            new("Foo", 1337), 
            new("Foo", 1337), 
            new("Bar", 42),
            new("Bar", 42)
        });
    }
}