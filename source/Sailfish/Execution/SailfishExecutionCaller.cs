using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;

namespace Sailfish.Execution;

internal static class SailfishExecutionCaller
{
    [Obsolete("This method is no longer recommended. Instead, implement IProvideARegistrationCallback and let it be auto-discovered")]
    internal static async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null, CancellationToken? cancellationToken = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        registerAdditionalTypes?.Invoke(builder);

        return await builder.Build().Resolve<SailfishExecutor>().Run(runSettings, cancellationToken ?? default).ConfigureAwait(false);
    }

    internal static async Task<SailfishValidity> Run(
        RunSettings runSettings,
        CancellationToken? cancellationToken = default)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        var container = builder.Build();
        var executor = container.Resolve<SailfishExecutor>();
        var result = await executor.Run(runSettings, cancellationToken ?? default).ConfigureAwait(false);

        return result;
    }
}