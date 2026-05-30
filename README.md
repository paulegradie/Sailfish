# Sailfish

[![Build Pipeline v4.0](https://github.com/paulegradie/Sailfish/actions/workflows/build-v4.0.yml/badge.svg)](https://github.com/paulegradie/Sailfish/actions/workflows/build-v4.0.yml)
![NuGet](https://img.shields.io/nuget/dt/Sailfish)
[![codecov](https://codecov.io/gh/paulegradie/Sailfish/graph/badge.svg?token=UN17VRVD0N)](https://codecov.io/gh/paulegradie/Sailfish)

Sailfish is a .NET performance testing framework that makes it easy to write, run, and analyze performance tests with statistical rigor.

- Documentation: https://paulgradie.com/Sailfish/
- NuGet: https://www.nuget.org/packages/Sailfish/

---

## Table of Contents
- [Features](#features)
- [Quick Start](#quick-start)
- [Algorithm Comparisons](#algorithm-comparisons)
- [Adaptive Sampling](#adaptive-sampling)
- [Installation](#installation)
- [IDE setup](#ide-setup)
- [Outputs and Reporting](#outputs-and-reporting)
- [Used By](#used-by)
- [Contributing](#contributing)
- [License](#license)

---

## Features
- Statistical rigor: outlier detection, multiple tests, significance analysis
- **Method comparisons by default** — every `[SailfishMethod]` in a `[Sailfish]` class is automatically compared; opt out per class with `[Sailfish(DisableComparison = true)]`
- Multiple outputs: test logs, Markdown, CSV
- Easy CI/CD integration
- Historical analysis with SailDiff
- Timer calibration with 0–100 Jitter Score; shows in Markdown header, manifest, and Environment Health
- Seeded run order (opt‑in): Deterministic ordering across classes, methods, and variable sets when a seed is provided; seed appears in Markdown header and manifest


- Highly configurable

## Quick Start

```csharp
[Sailfish]
public class MyPerformanceTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Your code to test
        Thread.Sleep(10);
    }
}
```

## Algorithm Comparisons
Every `[SailfishMethod]` on a `[Sailfish]` class is automatically compared against its siblings — no extra attributes required. Pick a baseline with `IsBaseline = true` for an N−1 baseline-vs-contender report; without one you get the full N×N matrix. Both modes report ratios with 95% confidence intervals and BH-FDR–adjusted q-values.

```csharp
[WriteToMarkdown]  // Generate consolidated markdown output
[WriteToCsv]       // Generate consolidated CSV output
[Sailfish(SampleSize = 100)]
public class SortBenchmarks
{
    [SailfishMethod(IsBaseline = true)]
    public void QuickSort() { /* implementation */ }

    [SailfishMethod]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    public void MergeSort() { /* implementation */ }
}
```

If a class isn't really doing a comparison (a smoke test that times unrelated operations), set `[Sailfish(DisableComparison = true)]` and the methods run individually with no comparison output. For classes that need multiple distinct comparisons in one place, set `ComparisonGroup = "..."` on individual `[SailfishMethod]`s.

See [Method Comparisons](https://paulgradie.com/Sailfish/docs/1/method-comparisons) for the full feature, including explicit `ComparisonGroup` for multi-group classes and the SF1300/1301/1302 build-time analyzers.

## Adaptive Sampling
Adaptive sampling stops collecting samples once results are statistically stable (defaults shown):
- Coefficient of Variation (CV) ≤ 5%
- Relative Confidence Interval width ≤ 20% at 95% confidence

Attribute-based (per-class):
```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 1000)]
public class StableTiming
{
    [SailfishMethod]
    public async Task Work() => await Task.Delay(10);
}
```

Global configuration (all tests in a run):
```csharp
var runSettings = RunSettingsBuilder.CreateBuilder()
    .WithGlobalAdaptiveSampling(targetCoefficientOfVariation: 0.05, maximumSampleSize: 500)
    // other options like .WithGlobalSampleSize(...), .DisableOverheadEstimation(), etc.
    .Build();
```

Note: Global settings act as defaults/overrides and can be combined with attribute settings per class.


## Outlier Handling (defaults + opt‑in)
By default, Sailfish removes both upper and lower outliers (Tukey fences) to preserve historical behavior.

To opt into configurable outlier handling, set the flag on the `[Sailfish]` attribute (per class) or on `RunSettingsBuilder` (globally).

Per class:

````csharp
[Sailfish(
    UseConfigurableOutlierDetection = true,
    OutlierStrategy = OutlierStrategy.RemoveUpper // or RemoveLower, RemoveAll, DontRemove, Adaptive
)]
public class MyTest { }
````

Globally (overrides per-class values):

````csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)
    .Build();
````

- Legacy default (no behavior change): `UseConfigurableOutlierDetection = false` → `RemoveAll`
- Strategies:
  - `RemoveUpper`: remove only upper-fence outliers
  - `RemoveLower`: remove only lower-fence outliers
  - `RemoveAll`: remove both sides (legacy behavior)
  - `DontRemove`: keep all data points, still reporting detected outliers
  - `Adaptive`: choose based on which side(s) have detected outliers

## Reproducible Run Order (Seed)
Set a seed to make run order deterministic across test classes, methods, and variable sets:

````csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithSeed(12345) // deterministic ordering across classes, methods, and variable sets
    .Build();
````

- Legacy fallback: `.WithArg("seed", "12345")` is still honored if `Seed` is null
- The seed is surfaced in the Markdown header and in the Reproducibility Manifest

## Installation

Requires **.NET 9** or **.NET 10**.

Install via NuGet:

```bash
# For a class-library / programmatic runner
dotnet add package Sailfish

# For tests that should be discovered by Visual Studio / Rider Test Explorer
dotnet add package Sailfish.TestAdapter
```

## IDE setup

### Visual Studio
Works out of the box. Reference `Sailfish.TestAdapter` (NuGet or `ProjectReference`) and Test Explorer will discover tests after a build.

### JetBrains Rider
Rider needs a one-time setting flipped so it engages VSTest discovery for projects that use Sailfish (or any third-party VSTest adapter without a built-in ReSharper provider).

1. `Settings → Build, Execution, Deployment → Unit Testing → VSTest`
2. Confirm **Enable VSTest adapters support** is on.
3. Under **Projects with unit tests**, click `+` and add a file mask. Use `*` to opt every project in the solution in (recommended), or a specific name like `*PerformanceTests*` to narrow.
4. Save → `Build → Rebuild Solution`.

Sailfish tests then appear in the Unit Tests tool window with gutter play-buttons, and parameterized variants nest under their parent `SailfishMethod` (via the `TestCase.Hierarchy` properties the adapter emits).

**Why the mask is needed**: ReSharper has built-in providers for xUnit / NUnit / MSTest only. For other VSTest adapters, it relies on the project being explicitly opted in to VSTest discovery via this mask — without it, ReSharper treats the project as non-test and greys out the run command. See issue [#98](https://github.com/paulegradie/Sailfish/issues/98) for background.

## Outputs and Reporting
- Per-group method comparisons (N−1 baseline mode or full N×N matrix)
- BH-FDR–adjusted q-values and 95% ratio confidence intervals
- Consolidated Markdown and CSV outputs
- Improved / Slower / Similar labels at α = 0.05

See the full documentation for output details and examples: https://paulgradie.com/Sailfish/

## Used By
<p>
  <img src="./assets/OctopusDeploy-logo-RGB.svg" alt="Octopus Deploy" width="320" />
</p>
<p>
  <img src="./assets/empower.svg" alt="Empower" width="320" />
</p>

## Contributing
Contributions are welcome! Feel free to open issues and pull requests.

## License
Sailfish is released under the [MIT license](LICENSE).
