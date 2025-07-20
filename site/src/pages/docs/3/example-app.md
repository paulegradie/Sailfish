---
title: Example App Setup
---

This guide demonstrates how to set up a complete Sailfish application with both console and IDE execution capabilities. You'll learn how to structure projects for maximum flexibility.

{% info-callout title="Hybrid Approach" %}
This example shows how to create a setup that allows you to execute tests from both a console application and your IDE, giving you the best of both worlds.
{% /info-callout %}

## 🖥️ Console Application Project

{% success-callout title="MyApp.CLI Project" %}
The console application provides programmatic control over test execution and is perfect for CI/CD pipelines and automated testing scenarios.
{% /success-callout %}

### Project Configuration

**PerformanceTests.csproj**
```xml
<ProjectReference Include="..\MyApp.PerformanceTests\PerformanceTests.csproj" />
```

### Application Code

```csharp
// .NET 6 Program.cs

var sourceTypesProvider = new Type[] { typeof(RegistrationProvider) };
var registrationProviderTypesProvider = new Type[] { typeof(RegistrationProvider) };

var sailfishRunResult = await SailfishRunner.Run(
    AssembleRunRequest(
        args,
        sourceTypesProvider,
        registrationProviderTypesProvider),
    cancellationToken);

var status = sailfishRunResult.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {status}valid");

RunSettings AssembleRunRequest(string[] args)
{
    return RunSettingsBuilder
        .CreateBuilder()
        .WithProvidersFromAssembliesContaining(typeof(RegistrationProvider))
        .Build();
}

public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.Register(_ => typeInstance).As<IClient>();
    }
}
```

## 🧪 Performance Tests Project

{% tip-callout title="MyApp.PerformanceTests Project" %}
The performance tests project contains your actual test classes and can be executed directly from your IDE with full debugging support.
{% /tip-callout %}

### Project Configuration

**PerformanceTests.csproj**
```xml
<PackageReference Include="Sailfish.TestAdapter" Version="{Latest}" />
```

### Test Implementation

```csharp
// This will be picked up by Sailfish's dependency scanner
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.Register(_ => typeInstance).As<IClient>();
    }
}

// This is runnable via the IDE
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

## 🚀 Benefits of This Architecture

{% feature-grid columns=2 %}
{% feature-card title="IDE Integration" description="Run and debug tests directly from Visual Studio, VS Code, or Rider with full breakpoint support." /%}

{% feature-card title="Console Execution" description="Execute tests programmatically for CI/CD pipelines, automation, and custom workflows." /%}

{% feature-card title="Shared Dependencies" description="Use the same dependency registration across both execution modes for consistency." /%}

{% feature-card title="Flexible Deployment" description="Deploy as a console app for production monitoring or use in development environments." /%}
{% /feature-grid %}

{% success-callout title="Best of Both Worlds" %}
This design allows you to execute tests from the console app for automation and CI/CD, as well as from the IDE for development and debugging.
{% /success-callout %}

## 📋 Project Structure

```
MyApp/
├── MyApp.CLI/
│   ├── Program.cs
│   └── MyApp.CLI.csproj
└── MyApp.PerformanceTests/
    ├── AMostBasicTest.cs
    ├── RegistrationProvider.cs
    └── PerformanceTests.csproj
```

{% note-callout title="Next Steps" %}
Ready to customize further? Explore [Extensibility](/docs/3/extensibility) to learn about custom handlers, output formats, and advanced configuration options.
{% /note-callout %}
