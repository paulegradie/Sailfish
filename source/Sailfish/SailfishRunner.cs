using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Execution;
using Sailfish.Utils;

namespace Sailfish;

/// <summary>
///     This is the main entry point to Sailfish when executing as a console app or tool
/// </summary>
public static class SailfishRunner
{
    [Warning(
        "This method is not generally recommended, because registrations passed this way are not available via the IDE Test Adapter. Only use this if your project doesn't require IDE play button functionality. In general, prefer to implement IProvideARegistrationCallback and let it be auto-discovered")]
    public static async Task<SailfishRunResult> Run(
        IRunSettings runSettings,
        Action<ContainerBuilder> registerAdditionalTypes,
        CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, registerAdditionalTypes, cancellationToken);
    }

    public static async Task<SailfishRunResult> Run(IRunSettings runSettings, CancellationToken cancellationToken = default)
    {
        return await SailfishExecutionCaller.Run(runSettings, null, cancellationToken);
    }
}