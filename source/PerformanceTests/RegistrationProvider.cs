using System.Threading.Tasks;
using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Registration;
using Serilog;

namespace PerformanceTests;

public class RegistrationProvider : IProvideRegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}