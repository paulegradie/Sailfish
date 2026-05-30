using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 3, NumWarmupIterations = 2, DisableOverheadEstimation = false, Disabled = false, DisableComparison = true)]
public sealed class SailfishFixtureExample : TestBase
{
    private readonly SailfishFixture _sailfishFixture;

    public ExampleDep ExampleDep = null!;

    public SailfishFixtureExample(SailfishFixture sailfishFixture, WebApplicationFactory<DemoApp> factory) : base(factory)
    {
        _sailfishFixture = sailfishFixture;
    }

    [SailfishVariable(1, 10)]
    public int VariableA { get; set; }

    [SailfishRangeVariable(true, 1, 4)]
    public int Multiplier { get; set; }

    [SailfishMethod]
    public async Task Control(CancellationToken cancellationToken)
    {
        await Task.Delay(VariableA * Multiplier, cancellationToken);
    }

    [SailfishMethodSetup(nameof(TestB))]
    public void ResolveSetup()
    {
        ExampleDep = _sailfishFixture.ResolveType<ExampleDep>();
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        ExampleDep.WriteSomething("Hello", VariableA * Multiplier);
    }
}

public class ExampleDep
{
    public void WriteSomething(string something, int sleepPeriod)
    {
        Thread.Sleep(sleepPeriod);
    }
}

public class TestBase : ISailfishFixture<SailfishFixture>
{
    public TestBase(WebApplicationFactory<DemoApp> factory)
    {
        WebHostFactory = factory.WithWebHostBuilder(
            builder => { builder.UseTestServer(); });
        Client = WebHostFactory.CreateClient();
    }

    public WebApplicationFactory<DemoApp> WebHostFactory { get; set; }
    public HttpClient Client { get; }

    public virtual async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Client.GetAsync("api", cancellationToken);
    }
}

// Example of what can be implemented
public class SailfishFixture : IDisposable
{
    // single parameterless ctor is all that is allowed
    public SailfishFixture()
    {
        var services = new ServiceCollection();
        RegisterThings(services);
        Provider = services.BuildServiceProvider();
    }

    private ServiceProvider Provider { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Provider.Dispose();
    }

    private static void RegisterThings(IServiceCollection services)
    {
        services.AddTransient<ExampleDep>();
        services.AddTransient<WebApplicationFactory<DemoApp>>();
    }

    public object ResolveType(Type type)
    {
        return Provider.GetRequiredService(type);
    }

    public T ResolveType<T>() where T : notnull
    {
        return Provider.GetRequiredService<T>();
    }
}