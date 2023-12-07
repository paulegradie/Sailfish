using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Registration;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests;

public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
    }
}