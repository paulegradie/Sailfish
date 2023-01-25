using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Execution;
using Sailfish.Registration;

namespace Sailfish;

/// <summary>
///     This is the main entry point to Sailfish when executing as a console app or tool
/// </summary>
public static class SailfishRunner
{
    [Obsolete("This method is no longer recommended. Instead, implement IProvideARegistrationCallback and let it be auto-discovered")]
    public static async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null, CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, registerAdditionalTypes, cancellationToken);
    }

    public static async Task<SailfishValidity> Run(RunSettings runSettings, CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, cancellationToken);
    }
}