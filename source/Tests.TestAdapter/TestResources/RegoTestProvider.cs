using Autofac;
using Sailfish.Registration;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.TestAdapter.TestResources;

public class RegoTestProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterType<GenericDependency<AnyType>>();
    }
}