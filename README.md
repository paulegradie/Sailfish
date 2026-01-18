# Sailfish

[![Build Pipeline v3.0](https://github.com/paulegradie/Sailfish/actions/workflows/build-v3.0.yml/badge.svg)](https://github.com/paulegradie/Sailfish/actions/workflows/build-v3.0.yml)
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
- [Outputs and Reporting](#outputs-and-reporting)
- [Used By](#used-by)
- [Contributing](#contributing)
- [License](#license)

---

## Features
- Statistical rigor: outlier detection, multiple tests, significance analysis
- Method comparisons with `[SailfishComparison]`
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
Compare multiple algorithms automatically and get N×N matrices, significance tests, and clear performance ratios.

```csharp
[WriteToMarkdown]  // Generate consolidated markdown output
[WriteToCsv]       // Generate consolidated CSV output
[Sailfish(SampleSize = 100)]
public class AlgorithmComparison
{
    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void BubbleSort() { /* implementation */ }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithms")]
    public void QuickSort() { /* implementation */ }
}
```

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

To opt into configurable outlier handling per run, enable the flag on ExecutionSettings and choose a strategy:

````csharp
var settings = new ExecutionSettings(asCsv: false, asConsole: true, asMarkdown: true, sampleSize: 20, numWarmupIterations: 0)
{
    UseConfigurableOutlierDetection = true,
    OutlierStrategy = OutlierStrategy.RemoveUpper // or RemoveLower, RemoveAll, DontRemove, Adaptive
};

// When converting programmatically
var result = PerformanceRunResult.ConvertFromPerfTimer(testCaseId, performanceTimer, settings);
````

- Legacy default (no behavior change): UseConfigurableOutlierDetection = false → RemoveAll
- Strategies:
  - RemoveUpper: remove only upper-fence outliers
  - RemoveLower: remove only lower-fence outliers
  - RemoveAll: remove both sides (legacy behavior)
  - DontRemove: keep all data points, still reporting detected outliers
  - Adaptive: choose based on which side(s) have detected outliers


Global configuration for an entire run via RunSettingsBuilder:

````csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)
    .Build();
````

Note: Attribute-level knobs for outlier strategy are not yet exposed; programmatic configuration is available for advanced scenarios and runners that surface ExecutionSettings.

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
Install via NuGet:

```bash
dotnet add package Sailfish
```

## Outputs and Reporting
- N×N method comparison matrices per comparison group
- Statistical significance testing (p-values, confidence intervals)
- Consolidated Markdown and CSV outputs
- Clear performance ratios (e.g., "X times faster/slower")

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
This project is licensed. See the LICENSE file in the repository for details.
