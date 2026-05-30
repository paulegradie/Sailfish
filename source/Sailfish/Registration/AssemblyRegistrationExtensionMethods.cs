using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    /// <summary>
    ///     Register every service Sailfish needs onto <paramref name="services" />. This is the entry point
    ///     for adding Sailfish to an MS DI container.
    /// </summary>
    public static IServiceCollection AddSailfish(this IServiceCollection services, IRunSettings runSettings)
    {
        return services.AddSailfishCore(runSettings);
    }
}
