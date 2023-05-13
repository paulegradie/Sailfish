---
title: Using the ISailfishFixture
---

The `ISailfishFixture` interface is intended to facilitate an xUnit-like experience, so its behavior is much the same.

```csharp
public class MyDependency
{
    public MyDependency() // must be parameterless
    {
        // do synchronous things as part of your setup
        lazyClient = new Lazy<IClient>(async () => await ClientFactory.Create());
    }

    private Lazy<IClient> lazyClient;
    public async Task<IClient> GetClient() => lazyClient.Value;
}

[Sailfish]
public class AMostBasicTest : ISailfishFixture<MyDependency>
{
    private readonly myDependency;

    public AMostBasicTest(MyDependency dependency)
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

## Requirements of the ISailfishFixture generic argument

The generic argument provided to the fixture interface must be a public class with a parameterless constructor. This type will be registered with Sailfish's inner container, making it available for dependency injection.
