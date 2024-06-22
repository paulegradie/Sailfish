using Autofac;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    public static void RegisterSailfishTypes(this ContainerBuilder builder, IRunSettings runSettings)
    {
        new SailfishModuleRegistrations(runSettings).Load(builder);
    }

    internal static void RegisterSailfishTypes(
        this ContainerBuilder builder,
        IRunSettings runSettings,
        params IProvideAdditionalRegistrations[] additionalModules)
    {
        builder.RegisterSailfishTypes(runSettings);
        foreach (var additionalModule in additionalModules) additionalModule.Load(builder);
    }
}