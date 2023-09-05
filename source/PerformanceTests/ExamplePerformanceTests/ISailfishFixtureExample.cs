using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2, Disabled = false)]
public class ISailfishFixtureExample : TestBase
{
    private readonly SailfishDependencies sailfishDependencies;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task Control(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }

    public ExampleDep exampleDep = null!;

    [SailfishMethodSetup(nameof(TestB))]
    public void ResolveSetup()
    {
        exampleDep = sailfishDependencies.ResolveType<ExampleDep>();
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        exampleDep.WriteSomething("Hello");
    }

    public ISailfishFixtureExample(SailfishDependencies sailfishDependencies, WebApplicationFactory<DemoApp> factory) : base(factory)
    {
        this.sailfishDependencies = sailfishDependencies;
    }
}

enum Cat
{
    A,
    B
}

public class ExampleDep
{
    public void WriteSomething(string something)
    {
        Console.WriteLine(something);
        Thread.Sleep(100);
    }
}

public class TestBase : ISailfishFixture<SailfishDependencies>
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
public class SailfishDependencies : IDisposable
{
    public IContainer Container { get; set; }

    // single parameterless ctor is all that is allowed
    public SailfishDependencies()
    {
        var builder = new ContainerBuilder();
        RegisterThings(builder);
        Container = builder.Build();
    }

    private static void RegisterThings(ContainerBuilder builder)
    {
        builder.RegisterType<ExampleDep>().AsSelf();
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
    }

    public void Dispose()
    {
        Container.Dispose();
    }

    public object ResolveType(Type type)
    {
        return Container.Resolve(type);
    }

    public T ResolveType<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }
}