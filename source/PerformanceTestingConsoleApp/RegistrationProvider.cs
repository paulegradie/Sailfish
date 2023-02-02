using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Registration;
using Serilog;

namespace PerformanceTestingConsoleApp;

public class RegistrationProvider : IProvideARegistrationCallback
{
    public void Register(ContainerBuilder builder)
    {
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}