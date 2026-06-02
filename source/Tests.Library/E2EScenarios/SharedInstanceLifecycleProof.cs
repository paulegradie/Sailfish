using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Attributes;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.E2EScenarios;

/// <summary>
///     Proves the default <see cref="SailfishLifetime.SharedInstance" /> contract: for a class with multiple
///     methods AND multiple variable values (here 2 × 5 = 10 cases), the constructor and
///     <see cref="SailfishGlobalSetupAttribute" /> run <b>exactly once</b> — not once per case / per variable —
///     and <see cref="SailfishGlobalTeardownAttribute" /> runs once at the end. This is the guarantee that lets
///     expensive shared setup (seed a database, prewarm a cache) happen a single time per class.
/// </summary>
public class SharedInstanceLifecycleProof
{
    private static int _ctorCount;
    private static int _globalSetupCount;
    private static int _globalTeardownCount;

    // Default lifetime is SharedInstance (no Lifetime specified).
    [Sailfish(SampleSize = 1, NumWarmupIterations = 0)]
    public class SharedProbe
    {
        public SharedProbe() => Interlocked.Increment(ref _ctorCount);

        [SailfishVariable(1, 2, 3, 4, 5)]
        public int N { get; set; }

        [SailfishGlobalSetup]
        public void GlobalSetup() => Interlocked.Increment(ref _globalSetupCount);

        [SailfishMethod]
        public void A() { }

        [SailfishMethod]
        public void B() { }

        [SailfishGlobalTeardown]
        public void GlobalTeardown() => Interlocked.Increment(ref _globalTeardownCount);
    }

    [Fact]
    public async Task SharedInstance_RunsCtorAndGlobalSetupOncePerClass_NotPerCase()
    {
        _ctorCount = 0;
        _globalSetupCount = 0;
        _globalTeardownCount = 0;

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .TestsFromAssembliesContaining(typeof(SharedProbe))
            .ProvidersFromAssembliesContaining(typeof(SharedProbe))
            .WithTestNames(typeof(SharedProbe).FullName!)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeTrue();

        // 2 methods × 5 variable values = 10 cases — yet the expensive bits run ONCE for the whole class.
        _ctorCount.ShouldBe(1);
        _globalSetupCount.ShouldBe(1);
        _globalTeardownCount.ShouldBe(1);
    }
}
