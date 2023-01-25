using Autofac;

namespace Sailfish.Registration;

public interface IProvideARegistrationCallback
{
    void Register(ContainerBuilder builder);
}