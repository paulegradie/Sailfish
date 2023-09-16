---
title: Registering Tests Dependencies
---
Simlply implement one of the following three interfaces.

Sailfish will scan the calling assembly by default to discover implementations of this interface. You can customize the search location by providing an anchor type to the IRunSettings builder.

# IProvideARegistrationCallback

Implement the `IProvideARegistrationCallback` interface, which provides access to an autofac container builder. The container will hold and resolve your dependencies from a separate lifetime scope from Sailfish.

```csharp
var runSettings = RunSettingsBuilder
    .ProvidersFromAssembliesContaining(typeof(RegistrationProvider))
    .Build();

public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.Register(ctx => typeInstance).As<IClient>();
       builder.RegisterType<MyType>().AsSelf();
    }
}
```
---

# ISailfishDependency

```csharp
public class MyDependency : ISailfishDependency
{
    public void Print()
    {
        Console.WriteLine("Hello Wolrd");
    }
}
```
---

# ISailfishFixture

The `ISailfishFixture` interface is intended to facilitate an xUnit-like experience, so its behavior is much the same.

```csharp
public class MyDependency
{
    public MyDependency() // must be parameterless
    {
        // do synchronous things as part of your setup
        lazyClient = new Lazy<IClient>(
            async () => await ClientFactory.Create());
    }

    private Lazy<IClient> lazyClient;
    public async Task<IClient> GetClient() => lazyClient.Value;
}

[Sailfish]
public class Example : ISailfishFixture<MyDependency>
{
    private readonly myDependency;

    public Example(MyDependency dependency)
    {
        this.myDependency = dependency;
    }

    public IClient Client { get; set; }

    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken ct);
    {
        Client = await myDependency.GetClient()
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken)
    {
        await Client.Get("/api", cancellationToken);
    }
}
```

## Requirements of the ISailfishFixture generic type argument

 - public class
 - single parameterless constructor
