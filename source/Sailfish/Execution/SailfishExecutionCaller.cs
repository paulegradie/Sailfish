using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Registration;
using Sailfish.Registration.Bridge;
using Sailfish.Utils;

namespace Sailfish.Execution;

internal static class SailfishExecutionCaller
{
    [Warning(
        "Try avoid using the 'configureServices' callback because registrations passed this way are not available via the IDE Test Adapter. Only use this if your project doesn't require IDE play button functionality. In general, prefer to implement IRegisterSailfishServices and let it be auto-discovered.")]
    internal static async Task<SailfishRunResult> Run(
        IRunSettings runSettings,
        Action<IServiceCollection>? configureServices,
        CancellationToken? cancellationToken = null)
    {
        return await RunInner(runSettings, configureServices, legacyBuilderAction: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Legacy back-compat path — accepts an Autofac <c>Action&lt;ContainerBuilder&gt;</c>. The action's
    ///     registrations are bridged into MS DI via <see cref="AutofacBridge"/>.
    /// </summary>
    [Warning(
        "Try avoid using the 'registerAdditionalTypes' callback because registrations passed this way are not available via the IDE Test Adapter. Only use this if your project doesn't require IDE play button functionality. In general, prefer to implement IRegisterSailfishServices and let it be auto-discovered.")]
    internal static async Task<SailfishRunResult> RunLegacy(
        IRunSettings runSettings,
        Action<ContainerBuilder>? registerAdditionalTypes,
        CancellationToken? cancellationToken = null)
    {
        return await RunInner(runSettings, configureServices: null, legacyBuilderAction: registerAdditionalTypes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SailfishRunResult> RunInner(
        IRunSettings runSettings,
        Action<IServiceCollection>? configureServices,
        Action<ContainerBuilder>? legacyBuilderAction,
        CancellationToken? cancellationToken = null)
    {
        var services = new ServiceCollection();
        services.AddSailfish(runSettings);

        // Inline customisation (new MS DI path).
        configureServices?.Invoke(services);

        // Legacy Autofac inline customisation — bridge it.
        if (legacyBuilderAction is not null)
        {
            AutofacBridge.BridgeBuilderAction(services, legacyBuilderAction);
        }

        await SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
            services,
            runSettings.TestLocationAnchors,
            runSettings.RegistrationProviderAnchors,
            cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

        var ct = cancellationToken ?? CancellationToken.None;

        await using var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<SailfishExecutor>().Run(ct).ConfigureAwait(false);
    }
}
