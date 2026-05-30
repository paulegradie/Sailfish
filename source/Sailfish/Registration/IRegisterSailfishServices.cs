using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sailfish.Registration;

/// <summary>
///     Implement this interface in your test project (or any assembly listed as a registration anchor) to contribute
///     additional service registrations into Sailfish's DI container. Implementations are auto-discovered by
///     <see cref="SailfishTypeRegistrationUtility" /> at run time and invoked once during startup.
/// </summary>
/// <remarks>
///     This is the recommended replacement for the legacy <see cref="IProvideARegistrationCallback" /> interface,
///     which is now [Obsolete]. The new interface accepts the standard
///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> and integrates cleanly with the
///     wider .NET ecosystem.
/// </remarks>
public interface IRegisterSailfishServices
{
    /// <summary>
    ///     Add any services your tests need to <paramref name="services" />. Sailfish builds the service provider
    ///     after every implementation of this interface has been invoked.
    /// </summary>
    Task RegisterAsync(
        IServiceCollection services,
        CancellationToken cancellationToken = default);
}
