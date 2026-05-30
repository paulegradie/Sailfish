using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Registration;
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
        var services = new ServiceCollection();
        services.AddSailfish(runSettings);

        // Inline customisation (MS DI path).
        configureServices?.Invoke(services);

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
