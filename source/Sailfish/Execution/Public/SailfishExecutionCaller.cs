using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;

namespace Sailfish.Execution.Public;

internal static class SailfishExecutionCaller
{
    internal static async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null, CancellationToken? cancellationToken = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        registerAdditionalTypes?.Invoke(builder);

        return await builder.Build().Resolve<SailfishExecutor>().Run(runSettings, cancellationToken ?? default).ConfigureAwait(false);
    }
}