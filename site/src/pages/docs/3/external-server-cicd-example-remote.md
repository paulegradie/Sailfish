---
title: External Server CI/CD Example
---

## Scenario

In some pipelines you want to run Sailfish tests against an environment that lives outside the build agent, such as:

- A long-lived performance testing environment
- A staging cluster that is deployed as part of the pipeline
- A developer machine running a local copy of the server

The recommended pattern is to keep tests in a separate `*.PerformanceTests` project and use a small console app to drive the Sailfish run on the target environment.

## Architecture

1. **Console runner** (for example `MyApp.CLI`) – a small app that:
   - Configures the base address or connection string for the target environment.
   - Builds a DI container with the same dependencies as your production app.
   - Calls `SailfishRunner.Run(...)` to execute performance tests.

2. **Performance test project** – contains `[Sailfish]` test classes that depend on interfaces like `IClient` instead of concrete HTTP clients.

3. **CI pipeline step** – deploys or points the console runner at the desired environment and executes it.

## Wiring the remote endpoint

Your `IProvideARegistrationCallback` implementation can decide where the tests point by reading configuration:

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
        var baseUrl = Environment.GetEnvironmentVariable("PERF_BASE_URL")
                      ?? "https://staging.myapp.example";

        var client = new MyClient(baseUrl);
        builder.RegisterInstance(client).As<IClient>();
    }
}
```

By changing `PERF_BASE_URL` in CI you can reuse the same test project to target different environments.

## Running from CI

A typical pipeline step looks like:

```bash
dotnet run --project ./MyApp.CLI/MyApp.CLI.csproj --configuration Release
```

The console app builds the run settings, executes the tests, and writes Sailfish outputs (markdown, CSV, tracking files) into the configured output directory. You can then publish that directory as a CI artefact or push the results into your own storage.

This pattern keeps your CI pipeline simple while giving you full control over where the tests execute and how dependencies are configured.

