using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.Analysis.Ai;
using Sailfish.Logging;
using Sailfish.TestAdapter;
using Xunit;
using IRunSettings = Sailfish.Contracts.Public.Models.IRunSettings;

namespace Tests.TestAdapter.Execution;

/// <summary>
///     Covers deliverable #2: AI analysis being a silent no-op is the core UX failure. The adapter must surface
///     a single, actionable warning when Skipper was enabled but produced nothing — either because no transport
///     is registered or because nothing triggered it this run.
/// </summary>
public class TestExecutorAiWarningsTests
{
    [Fact]
    public void Warns_WhenAiEnabledButNoTransportRegistered()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();

        TestExecutor.WarnIfAiConfiguredButInactive(
            BuildProvider(aiEnabled: true, new NoOpSailfishAgent(), new SkipperActivitySink()),
            frameworkHandle);

        frameworkHandle.Received().SendMessage(
            TestMessageLevel.Warning,
            Arg.Is<string>(s => s.Contains("AddSkipperTransport") && s.Contains("no Skipper transport is registered")));
    }

    [Fact]
    public void Warns_WhenAiEnabledWithTransportButNothingTriggeredSkipper()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();
        var realAgent = Substitute.For<ISailfishAgent>(); // a registered transport (not the NoOp default)

        TestExecutor.WarnIfAiConfiguredButInactive(
            BuildProvider(aiEnabled: true, realAgent, new SkipperActivitySink() /* never triggered */),
            frameworkHandle);

        frameworkHandle.Received().SendMessage(
            TestMessageLevel.Warning,
            Arg.Is<string>(s => s.Contains("did not fire this run") && s.Contains("scaleFish")));
    }

    [Fact]
    public void DoesNotWarn_WhenSkipperActuallyFired()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();
        var triggeredSink = new SkipperActivitySink();
        triggeredSink.RecordTriggered();

        TestExecutor.WarnIfAiConfiguredButInactive(
            BuildProvider(aiEnabled: true, Substitute.For<ISailfishAgent>(), triggeredSink),
            frameworkHandle);

        frameworkHandle.DidNotReceive().SendMessage(TestMessageLevel.Warning, Arg.Any<string>());
    }

    [Fact]
    public void DoesNotWarn_WhenAiIsDisabled()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();

        TestExecutor.WarnIfAiConfiguredButInactive(
            BuildProvider(aiEnabled: false, new NoOpSailfishAgent(), new SkipperActivitySink()),
            frameworkHandle);

        frameworkHandle.DidNotReceive().SendMessage(TestMessageLevel.Warning, Arg.Any<string>());
    }

    private static IServiceProvider BuildProvider(bool aiEnabled, ISailfishAgent agent, ISkipperActivitySink sink)
    {
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.RunAiAnalysis.Returns(aiEnabled);

        var services = new ServiceCollection();
        services.AddSingleton(runSettings);
        services.AddSingleton(agent);
        services.AddSingleton(sink);
        services.AddSingleton(Substitute.For<ILogger>());
        return services.BuildServiceProvider();
    }
}
