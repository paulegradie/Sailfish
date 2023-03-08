# Using The IProvideARegistrationCallback

> **Note**: When should I use this?
>
> - When using `Sailfish` in a deployable application context, this is the recommended way to register dependencies.

All you need to do is implement the `IProvideARegistrationCallback` interface.

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.RegisterType(typeInstance).As<IClient>();
    }
}
```

By default, Sailfish will scan the calling assembly to discover implementations of this interface.

## Specifying the right provider to Sailfish

> ### As A Test Project

If you are using Sailfish as a(n unreferenced) test project, then that is the assembly that will be scanned.

> ### As A Console application

Same as above.

> ### Multi-project

If you are using one project to hold your performance tests (as a test project) and a separate to hold a console app that reads types from the test project, then you may consider implementing `IProvideARegistrationCallback` in each of these projects. Define shared registration logic outside of the provider, or specify a base class.

---
### Next: [Using the ISailfishFixture](./using-the-ISailfishFixture.md)
