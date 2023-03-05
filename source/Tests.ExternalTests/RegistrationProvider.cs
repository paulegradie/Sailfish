using Autofac;
using Sailfish.Registration;
using Serilog;

namespace Tests.ExternalTests;

public class E2ETestRegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        builder.RegisterInstance(Log.Logger).As<ILogger>();
    }
}