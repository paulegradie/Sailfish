using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Sailfish.Mediation;

/// <summary>
///     DI wiring for Sailfish's in-house mediator. Registers <see cref="IPublisher" />/<see cref="ISender" />
///     and discovers the notification/request handlers in an assembly. This is the in-house replacement for
///     MediatR's <c>AddMediatR(...)</c> + assembly scan — no license key, no third-party types on the public
///     contract surface.
/// </summary>
public static class MediationServiceCollectionExtensions
{
    /// <summary>
    ///     Register the mediator and scan the Sailfish core assembly for its handlers. Other assemblies (the
    ///     TestAdapter, consumer test assemblies) register their own handlers explicitly or via
    ///     <see cref="RegisterSailfishHandlersFromAssembly" />.
    /// </summary>
    public static IServiceCollection AddSailfishMediation(this IServiceCollection services)
    {
        // Transient (like MediatR's default) so each Mediator captures the IServiceProvider of the scope it
        // is resolved into; both interface views map to the same implementation type.
        services.AddTransient<IPublisher, Mediator>();
        services.AddTransient<ISender, Mediator>();

        services.RegisterSailfishHandlersFromAssembly(typeof(Mediator).Assembly);
        return services;
    }

    /// <summary>
    ///     Discover every closed <see cref="INotificationHandler{TNotification}" /> and
    ///     <see cref="IRequestHandler{TRequest,TResponse}" /> implementation in <paramref name="assembly" /> and
    ///     register each against the handler interface(s) it implements (transient). Notifications resolve all
    ///     registrations; requests resolve the last (so a later registration overrides an earlier default).
    /// </summary>
    public static IServiceCollection RegisterSailfishHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType) continue;

                var definition = contract.GetGenericTypeDefinition();
                if (definition == typeof(INotificationHandler<>) || definition == typeof(IRequestHandler<,>))
                {
                    services.AddTransient(contract, type);
                }
            }
        }

        return services;
    }
}
