using System;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;

namespace Sailfish.Execution;

public static class SailfishExecutionCaller
{
    internal static async Task Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        if (registerAdditionalTypes is not null)
        {
            registerAdditionalTypes(builder);
        }

        var container = builder.Build();
        await container.Resolve<SailfishExecutor>().Run(runSettings);
    }
}