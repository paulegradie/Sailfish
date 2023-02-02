using System.Threading.Tasks;
using Autofac;

namespace Sailfish.Registration;

public interface IProvideARegistrationCallback
{
    void Register(ContainerBuilder builder);
}

public interface IProvideAsyncRegistrationCallback
{
    Task RegisterAsync(ContainerBuilder builder);
}