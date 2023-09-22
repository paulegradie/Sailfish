---
title: Example app setup
---

## MyApp.CLI (a project)

**PerformanceTests.csproj**

    <ProjectReference Include="..\MyApp.PerformanceTests\PerformanceTests.csproj" />

```csharp
// .net6 program.cs

var SourceTypesProvider = new Type[] { RegistrationProvider };
var RegistrationProviderTypesProvider = new Type[] { RegistrationProvider };

var sailfishRunResult = await SailfishRunner.Run(
            AssembleRunRequest(
                args,
                SourceTypesProvider,
                RegistrationProviderTypesProvider),
            cancellationToken);
var not = sailfishRunResult.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");

RunSettings AssembleRunRequest(args)
{
    return new RunSettings(...);
}

public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.RegisterType(typeInstance).As<IClient>();
    }
}
```

## MyApp.PerformanceTests (a project)

**PerformanceTests.csproj**

    <PackageReference Include="Sailfish.TestAdapter" Version={Latest} />

```csharp
// this will be picked up by Sailfish's dependency scanner
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.RegisterType(typeInstance).As<IClient>();
    }
}

// this is runnable via the IDE
[Sailfish]
public class AMostBasicTest
{
    private readonly IClient myClient;

    public AMostBasicTest(IClient myClient)
    {
        this.myClient = myClient;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken)
    {
        await myClient.Get("/api", cancellationToken);
    }
}
```

This design allows you to execute tests from the console app, as well as the IDE.
