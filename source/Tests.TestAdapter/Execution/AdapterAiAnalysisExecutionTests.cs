using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NSubstitute;
using Sailfish;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Registration;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Registrations;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Builders.ScaleFish;
using Tests.Common.Utils;
using Xunit;
using IRunSettings = Sailfish.Contracts.Public.Models.IRunSettings;

namespace Tests.TestAdapter.Execution;

/// <summary>
///     End-to-end coverage of the regression these changes fix: the VS Test Adapter's execution program must run
///     ScaleFish and fire Skipper exactly like the programmatic <c>SailfishExecutor.Run</c>. Drives
///     <see cref="ITestAdapterExecutionProgram" /> (the adapter's run-completion path) with a stubbed engine /
///     complexity computer and a fake Skipper agent, then asserts the agent was invoked and the artifacts written.
/// </summary>
public class AdapterAiAnalysisExecutionTests
{
    [Fact]
    public async Task AdapterRun_WithScaleFishAndAiEnabledAndTransport_FiresSkipperAndWritesArtifacts()
    {
        var outputDir = NewTempDir();
        try
        {
            var runSettings = RunSettingsBuilder.CreateBuilder()
                .WithScaleFish()
                .WithAiAnalysis(new AiAnalysisSettings(useResponseCache: false))
                .WithLocalOutputDirectory(outputDir)
                .CreateTrackingFiles(false)
                .DisableOverheadEstimation()
                .Build();

            var agent = FakeAgentReturning(NonEmptyReview());
            await using var provider = BuildProvider(runSettings, agent);

            await provider.GetRequiredService<ITestAdapterExecutionProgram>()
                .Run(new List<TestCase> { ATestCase() }, CancellationToken.None);

            // Skipper fired: the registered transport/agent was actually called.
            await agent.Received().RunAsync(Arg.Any<SkipperSession>(), Arg.Any<CancellationToken>());

            // Artifacts were written beside the run output (keyed by the 'scalefish' analysis kind).
            Directory.GetFiles(outputDir, "skipper-review_*_scalefish.json").ShouldNotBeEmpty();
            Directory.GetFiles(outputDir, "skipper-report_*_scalefish.md").ShouldNotBeEmpty();
        }
        finally
        {
            TryDelete(outputDir);
        }
    }

    [Fact]
    public async Task AdapterRun_WithScaleFishButNoTransport_DoesNotFireAndWritesNoArtifacts()
    {
        var outputDir = NewTempDir();
        try
        {
            var runSettings = RunSettingsBuilder.CreateBuilder()
                .WithScaleFish()
                .WithAiAnalysis(new AiAnalysisSettings(useResponseCache: false))
                .WithLocalOutputDirectory(outputDir)
                .CreateTrackingFiles(false)
                .DisableOverheadEstimation()
                .Build();

            // No agent registered → the core NoOpSailfishAgent default stays in place.
            await using var provider = BuildProvider(runSettings, agent: null);

            await provider.GetRequiredService<ITestAdapterExecutionProgram>()
                .Run(new List<TestCase> { ATestCase() }, CancellationToken.None);

            provider.GetRequiredService<ISailfishAgent>().ShouldBeOfType<NoOpSailfishAgent>();
            (Directory.Exists(outputDir) ? Directory.GetFiles(outputDir, "skipper-*") : Array.Empty<string>())
                .ShouldBeEmpty();
        }
        finally
        {
            TryDelete(outputDir);
        }
    }

    [Fact]
    public async Task AdapterRun_WithAiDisabled_DoesNotFireSkipperEvenWithTransport()
    {
        var outputDir = NewTempDir();
        try
        {
            var runSettings = RunSettingsBuilder.CreateBuilder()
                .WithScaleFish() // ScaleFish on, but AI analysis NOT enabled
                .WithLocalOutputDirectory(outputDir)
                .CreateTrackingFiles(false)
                .DisableOverheadEstimation()
                .Build();

            var agent = FakeAgentReturning(NonEmptyReview());
            await using var provider = BuildProvider(runSettings, agent);

            await provider.GetRequiredService<ITestAdapterExecutionProgram>()
                .Run(new List<TestCase> { ATestCase() }, CancellationToken.None);

            await agent.DidNotReceive().RunAsync(Arg.Any<SkipperSession>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            TryDelete(outputDir);
        }
    }

    // Builds a real AddSailfish + AddSailfishTestAdapter provider, then overrides the engine (so no real
    // benchmarks run) and the complexity computer (so ScaleFish deterministically yields a model), plus an
    // optional fake Skipper agent. Last registration wins, so these override the defaults.
    private static ServiceProvider BuildProvider(IRunSettings runSettings, ISailfishAgent? agent)
    {
        var services = new ServiceCollection();
        services.AddSailfish(runSettings);
        services.AddSailfishTestAdapter(Substitute.For<IFrameworkHandle>());

        var engine = Substitute.For<ITestAdapterExecutionEngine>();
        engine.Execute(Arg.Any<List<TestCase>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<IClassExecutionSummary> { ACannedSummary() }));
        services.AddSingleton(engine);

        var computer = Substitute.For<IComplexityComputer>();
        computer.AnalyzeComplexity(Arg.Any<List<IClassExecutionSummary>>())
            .ReturnsForAnyArgs(new List<ScalefishClassModel> { ACannedComplexityModel() });
        services.AddSingleton(computer);

        var converter = Substitute.For<IMarkdownTableConverter>();
        converter.ConvertScaleFishResultToMarkdown(Arg.Any<List<ScalefishClassModel>>())
            .ReturnsForAnyArgs("## ScaleFish\nO(n)");
        services.AddSingleton(converter);

        if (agent is not null) services.AddSingleton(agent);

        return services.BuildServiceProvider();
    }

    private static ISailfishAgent FakeAgentReturning(SkipperReview review)
    {
        var agent = Substitute.For<ISailfishAgent>();
        agent.RunAsync(Arg.Any<SkipperSession>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(review));
        return agent;
    }

    private static SkipperReview NonEmptyReview()
    {
        return new SkipperReview(
            SkipperVerdict.Improved,
            new[] { new Finding("Sut.Method", SkipperVerdict.Improved, "Scales linearly.", Array.Empty<string>(), 0.9) },
            Array.Empty<ProposedAction>(),
            "Linear scaling confirmed.",
            "# Skipper\nLinear scaling confirmed.");
    }

    private static IClassExecutionSummary ACannedSummary()
    {
        var testCaseId = Some.SimpleTestCaseId();
        var result = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).Build();
        var compiled = new CompiledTestCaseResult(testCaseId, Some.RandomString(), result);
        return new ClassExecutionSummary(typeof(AdapterAiAnalysisExecutionTests), new ExecutionSettings(), new[] { compiled });
    }

    private static ScalefishClassModel ACannedComplexityModel()
    {
        var propertyModel = ScaleFishPropertyModelBuilder.Create().Build();
        var methodModel = new ScaleFishMethodModel(Some.RandomString(), new[] { propertyModel });
        return new ScalefishClassModel(Some.RandomString(), Some.RandomString(), new[] { methodModel });
    }

    private static TestCase ATestCase()
    {
        return new TestCase("Sut.Method", new Uri("executor://sailfishexecutor/v1"), "source.dll");
    }

    private static string NewTempDir()
    {
        return Path.Combine(Path.GetTempPath(), "sf_adapter_ai_" + Guid.NewGuid().ToString("N"));
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch
        {
            /* best-effort cleanup */
        }
    }
}
