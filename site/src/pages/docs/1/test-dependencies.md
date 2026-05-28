---
title: Registering Tests Dependencies
---
Sailfish supports several ways to register dependencies that test classes can consume. Implement one of the interfaces below depending on the level of control you need.

Sailfish will scan the calling assembly by default to discover implementations. You can customize the search location by passing one or more anchor types to `RunSettingsBuilder.ProvidersFromAssembliesContaining(...)`.

# IProvideARegistrationCallback

Implement `IProvideARegistrationCallback` to get an Autofac `ContainerBuilder` callback. The container holds and resolves your dependencies from a lifetime scope managed by Sailfish.

```csharp
// In your Program.cs / Main
var runSettings = RunSettingsBuilder
    .CreateBuilder()
    .ProvidersFromAssembliesContaining(typeof(RegistrationProvider))
    .Build();
await SailfishRunner.Run(runSettings);

// In a class somewhere in the same assembly
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken cancellationToken = default)
    {
       var typeInstance = await MyClientFactory.Create(cancellationToken);
       builder.Register(ctx => typeInstance).As<IClient>();
       builder.RegisterType<MyType>().AsSelf();
    }
}
```
---

# IProvideAdditionalRegistrations

For purely synchronous registrations you can implement `IProvideAdditionalRegistrations`. Sailfish discovers and runs all implementations alongside any `IProvideARegistrationCallback` providers.

```csharp
public class AdditionalRegistrations : IProvideAdditionalRegistrations
{
    public void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MyType>().AsSelf();
    }
}
```
---

# ISailfishDependency

Mark a concrete type with `ISailfishDependency` to have it auto‑registered as itself. Use this when your dependency has a parameterless constructor and you just need it available for injection.

```csharp
public class MyDependency : ISailfishDependency
{
    public void Print()
    {
        Console.WriteLine("Hello World");
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
        lazyClient = new Lazy<Task<IClient>>(
            async () => await ClientFactory.Create());
    }

    private readonly Lazy<Task<IClient>> lazyClient;
    public Task<IClient> GetClient() => lazyClient.Value;
}

[Sailfish]
public class Example : ISailfishFixture<MyDependency>
{
    private readonly MyDependency myDependency;

    public Example(MyDependency dependency)
    {
        this.myDependency = dependency;
    }

    public IClient Client { get; set; }

    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken ct)
    {
        Client = await myDependency.GetClient();
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
