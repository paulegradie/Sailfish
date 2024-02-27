using Autofac;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Registration;

public interface IProvideARegistrationCallback
{
    Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken cancellationToken = default);
}