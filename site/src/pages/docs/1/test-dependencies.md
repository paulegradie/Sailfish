---
title: Registering Test Dependencies
---

Sailfish provides flexible dependency injection to support complex performance testing scenarios. Choose from three different approaches based on your needs.

{% info-callout title="Automatic Discovery" %}
Sailfish will scan the calling assembly by default to discover implementations of these interfaces. You can customize the search location by providing an anchor type to the IRunSettings builder.
{% /info-callout %}

## 🔧 Dependency Injection Options

{% feature-grid columns=3 %}
{% feature-card title="IProvideARegistrationCallback" description="Full control with Autofac container builder for complex scenarios." /%}

{% feature-card title="ISailfishDependency" description="Simple marker interface for automatic registration of dependencies." /%}

{% feature-card title="ISailfishFixture" description="xUnit-style fixture pattern for shared test resources." /%}
{% /feature-grid %}

## 🏗️ IProvideARegistrationCallback

{% success-callout title="Maximum Flexibility" %}
Implement the `IProvideARegistrationCallback` interface for full control over dependency registration using Autofac's container builder.
{% /success-callout %}

This approach provides access to an Autofac container builder. The container will hold and resolve your dependencies from a separate lifetime scope from Sailfish.

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

**Use cases:**
- **Complex Dependencies**: Multiple services with intricate relationships
- **Async Initialization**: Dependencies that require async setup
- **Factory Patterns**: Custom creation logic for dependencies
- **Third-party Integration**: Registering external services and clients

## 🏷️ ISailfishDependency

{% tip-callout title="Simple Registration" %}
Use the `ISailfishDependency` marker interface for automatic registration of simple dependencies with no special configuration needed.
{% /tip-callout %}

```csharp
public class MyDependency : ISailfishDependency
{
    public void Print()
    {
        Console.WriteLine("Hello World");
    }
}
```

**Use cases:**
- **Simple Services**: Stateless services with no complex dependencies
- **Utilities**: Helper classes and utility services
- **Quick Setup**: When you need minimal configuration overhead

## 🧪 ISailfishFixture

{% code-callout title="xUnit-Style Fixtures" %}
The `ISailfishFixture` interface provides an xUnit-like experience for shared test resources and setup logic.
{% /code-callout %}

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

### 📋 Requirements for ISailfishFixture

{% warning-callout title="Fixture Requirements" %}
The generic type argument for ISailfishFixture must meet specific requirements for proper instantiation.
{% /warning-callout %}

**Requirements:**
- **Public class** - Must be accessible for instantiation
- **Single parameterless constructor** - Sailfish needs to create instances automatically

**Use cases:**
- **Shared Resources**: Database connections, HTTP clients, test data
- **xUnit Migration**: Familiar pattern for developers coming from xUnit
- **Resource Management**: Automatic cleanup and disposal patterns

{% note-callout title="Next Steps" %}
Now that you understand dependency injection, explore the [Sailfish Tools](/docs/2/sailfish) to learn about SailDiff for regression testing and ScaleFish for complexity analysis.
{% /note-callout %}
