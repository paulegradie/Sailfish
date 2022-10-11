using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Execution;

namespace Sailfish;

/// <summary>
///     This is the main entry point to Sailfish when executing as a console app or tool
/// </summary>
public class SailfishExecution
{
    public async Task<SailfishValidity> Run(RunSettings runSettings, Action<ContainerBuilder>? registerAdditionalTypes = null, CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, registerAdditionalTypes, cancellationToken);
    }
}