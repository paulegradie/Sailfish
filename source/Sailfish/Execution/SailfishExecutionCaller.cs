using System;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;

namespace Sailfish.Execution;

public static class SailfishExecutionCaller
{
    internal static async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);

        registerAdditionalTypes?.Invoke(builder);

        var container = builder.Build();
        return await container.Resolve<SailfishExecutor>().Run(runSettings);
    }
}