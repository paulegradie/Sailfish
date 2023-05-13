---
title: As a Test Project
---

Using Sailfish as a test project is a simple way to encourage you and your fellow developers to write basic benchmarking tests when making changes to your code base.

## Setup

Setup up is simple: install the `Sailfish.TestAdapter` package from nuget.

Then, write a test:

```csharp
[Sailfish]
public class AMostBasicTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // do something you wish to time
    }
}
```

## Providing registrations to Sailfish when using the IDE test runner

There are various ways to register dependencies with Sailfish, so depending on how you intend to use sailfish, you may wish to consider which approach is more appropriate for your use case.

For more information on how to provide registrations to Sailfish, see our section on [Registering Dependencies for your tests](./../5/registering-dependencies-for-your-tests.md)

In this example, since we've focused on sailfish tests only in a test project context (.e.g, we're not using these tests elsewhere), we'll use the `ISailfishFixture<>`. You may notice that this is similar to the `IClassFixture<>` from xUnit.

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

With this setup, you will:

- register `MyDependency`
- create a lazy instance of the IClient implementation (created via the `ClientFactory`)
- Resolve the dependency in the test
- Set the Client instance as a property in the Global Setup [Lifecycle Method](../2/the-sailfish-test-lifecycle.md)
- Invoke the client in the SailfishMethod you wish to time
