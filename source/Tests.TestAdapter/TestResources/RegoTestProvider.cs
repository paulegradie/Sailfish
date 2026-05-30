using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Registration;

namespace Tests.TestAdapter.TestResources;

public class RegoTestProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        services.AddTransient<GenericDependency<AnyType>>();
    }
}
