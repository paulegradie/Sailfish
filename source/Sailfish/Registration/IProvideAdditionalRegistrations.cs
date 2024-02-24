using Autofac;

namespace Sailfish.Registration;

public interface IProvideAdditionalRegistrations
{
    void Load(ContainerBuilder builder);
}