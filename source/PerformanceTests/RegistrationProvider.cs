using System.Threading;
using System.Threading.Tasks;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Registration;

namespace PerformanceTests;

public class RegistrationProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(IServiceCollection services, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        services.AddTransient<WebApplicationFactory<DemoApp>>();
    }
}
