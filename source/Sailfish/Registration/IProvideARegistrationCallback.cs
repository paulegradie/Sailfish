using System.Threading;
using System.Threading.Tasks;
using Autofac;

namespace Sailfish.Registration;

public interface IProvideARegistrationCallback
{
    Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken cancellationToken = default);
}