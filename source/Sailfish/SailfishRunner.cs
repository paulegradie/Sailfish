using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish;

/// <summary>
///     This is the main entry point to Sailfish library.
/// </summary>
public static class SailfishRunner
{
    /// <summary>
    ///     Recommended: run Sailfish with no inline customisation. Any
    ///     <see cref="Sailfish.Registration.IRegisterSailfishServices"/> implementations in the assemblies
    ///     listed on <see cref="IRunSettings.RegistrationProviderAnchors"/> are auto-discovered.
    /// </summary>
    public static async Task<SailfishRunResult> Run(IRunSettings runSettings, CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, configureServices: null, cancellationToken);
    }

    /// <summary>
    ///     Run Sailfish with an inline MS DI customisation. The action runs after Sailfish's core
    ///     registrations land on the service collection and before the service provider is built.
    /// </summary>
    /// <remarks>
    ///     Note: registrations contributed this way are not available via the IDE Test Adapter (it runs in a
    ///     different process and doesn't see your inline action). For tests that need both, implement
    ///     <see cref="Sailfish.Registration.IRegisterSailfishServices"/> in an assembly listed on
    ///     <see cref="IRunSettings.RegistrationProviderAnchors"/> — that path is auto-discovered by both
    ///     entry points.
    /// </remarks>
    public static async Task<SailfishRunResult> Run(
        IRunSettings runSettings,
        Action<IServiceCollection> configureServices,
        CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, configureServices, cancellationToken);
    }
}
