using Autofac;

namespace AsAConsoleApp.Configuration;

public class RequiredAdditionalRegistrationsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Sailfish will need to resolve this type
        // builder.RegisterType<WebApplicationFactory<DemoApp>>();
        base.Load(builder);
    }
}