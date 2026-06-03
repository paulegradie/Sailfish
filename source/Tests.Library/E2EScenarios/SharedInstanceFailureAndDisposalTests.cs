using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish;
using Sailfish.Attributes;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.E2EScenarios;

/// <summary>
///     Regression tests for the SharedInstance failure paths surfaced in PR review, plus per-case scope disposal.
/// </summary>
public class SharedInstanceFailureAndDisposalTests
{
    private static int _methodRuns;
    private static int _scopedDisposeAsyncCount;

    // SharedInstance (default): a failing [SailfishGlobalSetup] must abort the whole class — no method may run on a
    // half-initialized instance — and surface exactly one (the GlobalSetup) exception rather than a per-method
    // cascade.
    [Sailfish(SampleSize = 1, NumWarmupIterations = 0)]
    public class GlobalSetupAbortProbe
    {
        [SailfishGlobalSetup]
        public void GlobalSetup() => throw new System.InvalidOperationException("global setup boom");

        [SailfishMethod]
        public void A() => Interlocked.Increment(ref _methodRuns);

        [SailfishMethod]
        public void B() => Interlocked.Increment(ref _methodRuns);
    }

    [Fact]
    public async Task GlobalSetupFailure_AbortsClass_MethodsDoNotRun_AndReportsOnce()
    {
        _methodRuns = 0;

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .TestsFromAssembliesContaining(typeof(GlobalSetupAbortProbe))
            .ProvidersFromAssembliesContaining(typeof(GlobalSetupAbortProbe))
            .WithTestNames(typeof(GlobalSetupAbortProbe).FullName!)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        _methodRuns.ShouldBe(0);             // GlobalSetup failed → neither method ran
        result.IsValid.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions!.Count().ShouldBe(1); // one clean failure, not one-per-method
    }

    public sealed class ScopedAsyncResource : System.IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _scopedDisposeAsyncCount);
            return ValueTask.CompletedTask;
        }
    }

    // PerCase: a scoped, async-only-disposable dependency must be disposed once per case (each case has its own DI
    // scope). Confirms the IAsyncDisposable-first disposal path on the per-case scope.
    [Sailfish(SampleSize = 1, NumWarmupIterations = 0, Lifetime = SailfishLifetime.PerCase)]
    public class ScopedDisposalProbe
    {
        private readonly ScopedAsyncResource _resource;

        public ScopedDisposalProbe(ScopedAsyncResource resource)
        {
            _resource = resource;
        }

        [SailfishVariable(1, 2, 3)]
        public int N { get; set; }

        [SailfishMethod]
        public void M() => _ = _resource;
    }

    [Fact]
    public async Task PerCase_ScopedAsyncDisposable_IsDisposedOncePerCase()
    {
        _scopedDisposeAsyncCount = 0;

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .TestsFromAssembliesContaining(typeof(ScopedDisposalProbe))
            .ProvidersFromAssembliesContaining(typeof(ScopedDisposalProbe))
            .WithTestNames(typeof(ScopedDisposalProbe).FullName!)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(
            runSettings,
            (IServiceCollection services) => services.AddScoped<ScopedAsyncResource>(),
            CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        _scopedDisposeAsyncCount.ShouldBe(3); // 3 variable values → 3 cases → 3 per-case scopes disposed
    }
}
