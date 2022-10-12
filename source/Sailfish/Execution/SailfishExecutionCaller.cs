using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;

namespace Sailfish.Execution;

public static class SailfishExecutionCaller
{
    internal static async Task<SailfishValidity> Run(RunSettings runSettings, Func<ContainerBuilder, CancellationToken, Task>? registerAdditionalTypes = null,
        CancellationToken cancellationToken = default)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        if (registerAdditionalTypes is not null)
        {
            await registerAdditionalTypes.Invoke(builder, cancellationToken).ConfigureAwait(false);
        }

        var container = builder.Build();
        return await container.Resolve<SailfishExecutor>().Run(runSettings, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null, CancellationToken? cancellationToken = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);
        builder.Register(ctx =>
                cancellationToken is not null ? new CancellationTokenAccess() { Token = cancellationToken } : new CancellationTokenAccess() { Token = new CancellationToken(false) })
            .AsSelf()
            .SingleInstance();

        registerAdditionalTypes?.Invoke(builder);

        var container = builder.Build();
        return await container.Resolve<SailfishExecutor>().Run(runSettings, cancellationToken ?? default).ConfigureAwait(false);
    }
}