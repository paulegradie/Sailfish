using System.Collections.Generic;
using NSubstitute;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Shouldly;
using Tests.Common.Builders.ScaleFish;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class SkipperScalingTests
{
    [Fact]
    public void BuildScaling_MapsBestFitRunnerUpAndGoodness()
    {
        var primary = new Linear();
        var secondary = new Quadratic();
        var model = ScaleFishModelBuilder.Create()
            .AddPrimaryFunction(primary).SetPrimaryGoodnessOfFit(0.99)
            .AddSecondaryFunction(secondary).SetSecondaryGoodnessOfFit(0.90)
            .Build();

        var notification = NotificationFor(model, methodName: "DoWork", propertyName: "N");

        var context = MakeBuilder().BuildScaling(notification);

        // Scaling-only context: no SailDiff comparisons, the ScaleFish markdown carried through.
        context.Comparisons.ShouldBeEmpty();
        context.SailDiffMarkdown.ShouldBe("## scalefish");
        context.Scaling.ShouldNotBeNull();

        var verdict = context.Scaling!.ShouldHaveSingleItem();
        verdict.TestMethodName.ShouldBe("DoWork");
        verdict.PropertyName.ShouldBe("N");
        verdict.BestFitComplexity.ShouldBe(primary.OName);
        verdict.GoodnessOfFit.ShouldBe(0.99);
        verdict.NextBestComplexity.ShouldBe(secondary.OName);
        verdict.NextBestGoodnessOfFit.ShouldBe(0.90);
        verdict.Projections.ShouldNotBeNull();
    }

    private static ScaleFishAnalysisCompleteNotification NotificationFor(ScaleFishModel model, string methodName, string propertyName)
    {
        var classModel = new ScalefishClassModel(
            "My.Namespace",
            "MyClass",
            new[] { new ScaleFishMethodModel(methodName, new[] { new ScaleFishPropertyModel(propertyName, model) }) });

        return new ScaleFishAnalysisCompleteNotification("## scalefish", new List<ScalefishClassModel> { classModel });
    }

    private static PerformanceNarrativeContextBuilder MakeBuilder()
    {
        return new PerformanceNarrativeContextBuilder(
            Substitute.For<IReproducibilityManifestProvider>(),
            Substitute.For<IEnvironmentHealthReportProvider>());
    }
}
