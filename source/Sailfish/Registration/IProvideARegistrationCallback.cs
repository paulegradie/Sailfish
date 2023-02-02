using System.Threading.Tasks;
using Autofac;

namespace Sailfish.Registration;

public interface IProvideRegistrationCallback
{
    Task RegisterAsync(ContainerBuilder builder);
}