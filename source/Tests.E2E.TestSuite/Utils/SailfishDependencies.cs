using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.E2E.TestSuite.Utils;

// Example of what a test author can implement: a parameterless-ctor class that builds its own DI container
// and exposes a typed resolver. Sailfish auto-resolves it when listed as a Sailfish fixture generic argument.
public class SailfishDependencies : IDisposable
{
    public SailfishDependencies()
    {
        var services = new ServiceCollection();
        RegisterThings(services);
        Provider = services.BuildServiceProvider();
    }

    private ServiceProvider Provider { get; }

    public void Dispose()
    {
        Provider.Dispose();
    }

    private static void RegisterThings(IServiceCollection services)
    {
        services.AddTransient<ExampleDep>();
        services.AddTransient<SailfishDependencies>();
        services.AddTransient<WebApplicationFactory<DemoApp>>();
    }

    public T ResolveType<T>() where T : notnull
    {
        return Provider.GetRequiredService<T>();
    }
}
