using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Registration;

namespace PerformanceTestingUserInvokedConsoleApp;

public class AppRegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        await Task.Yield();
    }
}