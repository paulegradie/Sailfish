---
title: Registering Tests Dependencies
---
Sailfish supports several ways to register dependencies that test classes can consume. Implement one of the interfaces below depending on the level of control you need.

Sailfish will scan the calling assembly by default to discover implementations. You can customize the search location by passing one or more anchor types to `RunSettingsBuilder.ProvidersFromAssembliesContaining(...)`.

Sailfish's DI container is `Microsoft.Extensions.DependencyInjection` (the standard .NET DI abstraction). You contribute services via an `IServiceCollection` — the same one you'd use in an ASP.NET Core app, a Generic Host, or any other modern .NET app.

# IRegisterSailfishServices

Implement `IRegisterSailfishServices` to register dependencies onto the `IServiceCollection` Sailfish will use to build its `IServiceProvider`. This is the primary extension point.

```csharp
// In your Program.cs / Main
var runSettings = RunSettingsBuilder
    .CreateBuilder()
    .ProvidersFromAssembliesContaining(typeof(RegistrationProvider))
    .Build();
await SailfishRunner.Run(runSettings);

// In a class somewhere in the same assembly
public class RegistrationProvider : IRegisterSailfishServices
{
    public async Task RegisterAsync(
        IServiceCollection services,
        CancellationToken cancellationToken = default)
    {
       var typeInstance = await MyClientFactory.Create(cancellationToken);
       services.AddSingleton<IClient>(typeInstance);
       services.AddTransient<MyType>();
    }
}
```

The implementing class must have a public parameterless constructor (Sailfish discovers and constructs it via reflection).

---

# Inline registration via `SailfishRunner.Run`

For one-off cases where you don't want a discoverable provider class, you can pass an `Action<IServiceCollection>` directly to `SailfishRunner.Run`. The action is invoked after Sailfish's core services are registered and before the `IServiceProvider` is built.

```csharp
await SailfishRunner.Run(
    runSettings,
    services =>
    {
        services.AddSingleton<IClient>(new MyClient());
        services.AddTransient<MyType>();
    });
```

Note: registrations added this way are *not* visible to the IDE Test Adapter, which runs in a separate process and never sees your `Action<IServiceCollection>`. If your tests need both the Sailfish runner and the IDE play button, prefer `IRegisterSailfishServices` — it's auto-discovered by both entry points.

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
