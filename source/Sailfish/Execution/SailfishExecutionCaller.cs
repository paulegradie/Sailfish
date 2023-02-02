﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Registration;
using Sailfish.Utils;

namespace Sailfish.Execution;

internal static class SailfishExecutionCaller
{
    [Warning(
        "Try avoid using the 'registerAdditionalTypes callback, because registrations passed this way are not available via the IDE Test Adapter. Only use this if your project doesn't require IDE play button functionality. In general, prefer to implement IProvideARegistrationCallback and let it be auto-discovered")]
    internal static async Task<SailfishRunResult> Run(
        RunSettings runSettings,
        Action<ContainerBuilder>? registerAdditionalTypes,
        CancellationToken? cancellationToken = null) => await RunInner(runSettings, registerAdditionalTypes, cancellationToken).ConfigureAwait(false);

    private static async Task<SailfishRunResult> RunInner(
        RunSettings runSettings,
        Action<ContainerBuilder>? registerAdditionalTypes = null,
        CancellationToken? cancellationToken = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterSailfishTypes();
        builder.RegisterPerformanceTypes(runSettings.TestLocationTypes);
        builder.RegisterInstance(runSettings).SingleInstance();
        registerAdditionalTypes?.Invoke(builder);

        CancellationToken ct;
        if (cancellationToken is null)
        {
            var source = new CancellationTokenSource();
            ct = source.Token;
        }
        else
        {
            ct = (CancellationToken)cancellationToken;
        }

        return await builder.Build().Resolve<SailfishExecutor>().Run(ct).ConfigureAwait(false);
    }
}