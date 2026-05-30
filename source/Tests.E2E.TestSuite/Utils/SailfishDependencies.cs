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
        // NB: we intentionally do NOT register SailfishDependencies into its own container —
        // calling Provider.GetRequiredService<SailfishDependencies>() would recursively instantiate
        // a new fixture (each of which builds its own ServiceProvider), so the registration was
        // both a latent stack-overflow hazard and dead code (the fixture instance is created by
        // Sailfish itself via ISailfishFixture<T>, not resolved through here).
        services.AddTransient<ExampleDep>();
        services.AddTransient<WebApplicationFactory<DemoApp>>();
    }

    public T ResolveType<T>() where T : notnull
    {
        return Provider.GetRequiredService<T>();
    }
}
