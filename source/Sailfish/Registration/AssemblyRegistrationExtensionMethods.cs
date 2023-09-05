using Autofac;

namespace Sailfish.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    public static void RegisterSailfishTypes(this ContainerBuilder builder, IRunSettings runSettings, params Module[] additionalModules)
    {
        builder.RegisterModule(new SailfishModule(runSettings));
        foreach (var additionalModule in additionalModules)
        {
            builder.RegisterModule(additionalModule);
        }
    }
}