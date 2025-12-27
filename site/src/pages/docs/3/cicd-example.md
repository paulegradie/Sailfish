---
title: CI/CD Example
---

## Goal

Run Sailfish performance tests as part of an automated CI/CD pipeline and publish the results as build artefacts.

This example assumes a separate `MyApp.PerformanceTests` project (see [Console App & Test Project](/docs/3/example-app)), but the pattern applies to any Sailfish test project.

## Basic pipeline shape

1. Restore and build your solution in Release mode.
2. Run `dotnet test` for the performance test project.
3. Collect the Sailfish output directory as an artefact.

By default Sailfish writes markdown, CSV, and tracking files into a folder named `sailfish_default_output` next to the test project. You can override this with `RunSettingsBuilder.WithLocalOutputDirectory(...)` when driving Sailfish from code.

## GitHub Actions sketch

```yaml
- name: Restore
  run: dotnet restore

- name: Build
  run: dotnet build --configuration Release

- name: Run Sailfish performance tests
  run: dotnet test ./MyApp.PerformanceTests/MyApp.PerformanceTests.csproj --configuration Release

- name: Publish Sailfish results
  uses: actions/upload-artifact@v4
  with:
    name: sailfish-results
    path: MyApp.PerformanceTests/sailfish_default_output
```

Adjust project paths to match your solution layout.

## Tagging CI runs

You can use tags to label CI runs (for example `branch`, `commit`, `environment`) when driving Sailfish via `SailfishRunner`:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithTag("branch", Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "local")
    .WithTag("commit", Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "dev")
    .Build();
```

Tags are encoded into the output file names and can be parsed later using `DefaultFileSettings.ExtractDataFromFileNameWithTagSection`.

## Environment considerations

For reliable CI numbers:

- Prefer dedicated performance agents or VMs over highly shared runners.
- Keep other workloads off the machine while Sailfish is running.
- Leave the environment health check and timer calibration enabled (the defaults) so issues are surfaced in the markdown and Test Output window.

