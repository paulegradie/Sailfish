---
title: Using the ISailfishDependency
---

Implementations of `ISailfishDependency` are automatical scanned for in the same way as IProvideARegistrationCallback - via the calling assemblies. If you passing an anchor types to the `RunSettings`, you can direct access to other assemblies by providing types from those assemblies to the `Runsettings.RegistrationProviderAnchors` property.

```csharp
public class MyDependency : ISailfishDependency
{
    public void Print()
    {
        Console.WriteLine("Hello Wolrd");
    }
}
```

This will be picked up and registered with the internal container. Since this works through Autofac's IContainer, you can register multiple dependencies this way, and inject them into each other before injecting this in to your test. For small scale projects, this a simple but powerful way to compose your registrations and tests.

**Warning: This approach is intended for simple projects that need to be stood up quickly. You may encounter maintenance overhead if your test project grows in size and complexity.**
