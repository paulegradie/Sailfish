using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.ExtensionMethods;
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
        builder.RegisterPerformanceTypes(runSettings.TestLocationAnchors);
        builder.RegisterInstance(runSettings).SingleInstance();
        registerAdditionalTypes?.Invoke(builder);

        // register the dependencies from the provider duh
        await InvokeRegistrationProviderCallback(builder, runSettings.TestLocationAnchors, runSettings.RegistrationProviderAnchors, cancellationToken ?? CancellationToken.None);

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

    private static async Task InvokeRegistrationProviderCallback(ContainerBuilder containerBuilder, IEnumerable<Type> testDiscoveryAnchorTypes, Type[] registrationProviderAnchorTypes,
        CancellationToken cancellationToken = default)
    {
        if (registrationProviderAnchorTypes == null) throw new ArgumentNullException(nameof(registrationProviderAnchorTypes));
        var allAssemblies = testDiscoveryAnchorTypes.Select(t => t.Assembly);
        var allAssemblyTypes = AssemblyScannerCache.GetTypesInAssemblies(allAssemblies.Distinct()).ToArray();

        var registrationProviderAssemblyTypes = registrationProviderAnchorTypes.SelectMany(t => t.Assembly.GetTypes()).Distinct();
        var asyncProviders = registrationProviderAssemblyTypes.GetRegistrationCallbackProviders<IProvideARegistrationCallback>().ToList();
        if (!asyncProviders.Any())
        {
            asyncProviders = allAssemblyTypes.GetRegistrationCallbackProviders<IProvideARegistrationCallback>().ToList();
        }

        foreach (var asyncCallback in asyncProviders)
        {
            var methodInfo = asyncCallback.GetType().GetMethod(nameof(IProvideARegistrationCallback.RegisterAsync));
            if (methodInfo is not null && methodInfo.IsAsyncMethod())
            {
                await asyncCallback.RegisterAsync(containerBuilder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var task = asyncCallback.RegisterAsync(containerBuilder, cancellationToken);
                if (!task.IsCompletedSuccessfully)
                {
                    throw new Exception("Task error in your registration callback");
                }
            }
        }
    }
}