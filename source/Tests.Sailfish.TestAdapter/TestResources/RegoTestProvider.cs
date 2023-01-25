using Autofac;
using Sailfish.Registration;

namespace Tests.Sailfish.TestAdapter.TestResources;

public class RegoTestProvider : IProvideARegistrationCallback
{
    public void Register(ContainerBuilder builder)
    {
        builder.RegisterType<GenericDependency<AnyType>>();
    }
}