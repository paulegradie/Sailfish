using System;
using Autofac;

namespace Sailfish.Registration;

/// <summary>
///     Legacy Autofac-typed module-style registration hook. Prefer
///     <see cref="IRegisterSailfishServices"/> for new code.
/// </summary>
/// <remarks>
///     Implementations of this interface are still discovered for backward compatibility; their Autofac
///     registrations are bridged into Sailfish's MS DI container via
///     <see cref="Bridge.AutofacBridge"/>. This interface and the bridge are scheduled for removal in a
///     future major release.
/// </remarks>
[Obsolete("Implement IRegisterSailfishServices instead. This Autofac-typed interface is bridged into MS DI for backward compatibility and will be removed in a future major release.", error: false)]
public interface IProvideAdditionalRegistrations
{
    void Load(ContainerBuilder builder);
}
