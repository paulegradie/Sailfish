using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Registration;
using Serilog;

namespace Tests.E2E.ExceptionHandling;

public class E2ETestExceptionHandlingProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}