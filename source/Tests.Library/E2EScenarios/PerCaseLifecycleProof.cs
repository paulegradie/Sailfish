using System;
using System.Collections.Generic;
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
///     Proves the v-next instance/lifecycle contract end-to-end through the public runner:
///     a DI <b>singleton</b> (e.g. a server held warm across the whole run) is the SAME instance for every
///     test case, while a <b>scoped</b> dependency (e.g. a DbContext) is a FRESH instance per case — because
///     each case is resolved from its own per-case DI scope. No instance state is copied between cases.
/// </summary>
public class PerCaseLifecycleProof
{
    // The run is in-process and sequential, so static collectors are safe to observe afterwards.
    private static readonly List<Guid> SingletonIds = new();
    private static readonly List<Guid> ScopedIds = new();

    public sealed class SharedServer
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public sealed class PerCaseResource
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [Sailfish(SampleSize = 1, NumWarmupIterations = 0, Lifetime = SailfishLifetime.PerCase)]
    public class LifecycleProbe
    {
        private readonly PerCaseResource _resource;
        private readonly SharedServer _server;

        public LifecycleProbe(SharedServer server, PerCaseResource resource)
        {
            _server = server;
            _resource = resource;
        }

        [SailfishVariable(1, 2, 3)]
        public int N { get; set; }

        [SailfishMethod]
        public void Probe()
        {
            SingletonIds.Add(_server.Id);
            ScopedIds.Add(_resource.Id);
        }
    }

    [Fact]
    public async Task SingletonIsSharedAcrossCases_ScopedIsFreshPerCase()
    {
        SingletonIds.Clear();
        ScopedIds.Clear();

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(Some.RandomString())
            .TestsFromAssembliesContaining(typeof(LifecycleProbe))
            .ProvidersFromAssembliesContaining(typeof(LifecycleProbe))
            .WithTestNames(typeof(LifecycleProbe).FullName!)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .Build();

        var result = await SailfishRunner.Run(
            runSettings,
            (IServiceCollection services) =>
            {
                services.AddSingleton<SharedServer>();   // shared, created once (a warm server)
                services.AddScoped<PerCaseResource>();   // fresh per case (a DbContext)
            },
            CancellationToken.None);

        result.IsValid.ShouldBeTrue();

        // Three variable values => three test cases, each invoked at least once.
        SingletonIds.Count.ShouldBeGreaterThanOrEqualTo(3);
        ScopedIds.Count.ShouldBeGreaterThanOrEqualTo(3);

        // The singleton is the SAME instance across every case — the server stays warm.
        SingletonIds.Distinct().Count().ShouldBe(1);

        // The scoped resource is a FRESH instance for each of the three cases — no leakage between cases.
        ScopedIds.Distinct().Count().ShouldBe(3);
    }
}
