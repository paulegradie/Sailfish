---
title: Reproducibility Manifest
---

## Introduction

Sailfish emits a Reproducibility Manifest alongside your session outputs. The manifest is a small JSON file that captures the key environment, configuration, and result summaries needed to reproduce a run with confidence.

- File name: `Manifest_<yyyyMMdd_HHmmss>.json`
- Location: the Run Settings Local Output Directory (defaults to `sailfish_default_output`)
- Visibility: referenced at the top of the consolidated Markdown (when available)

## What it captures

### Environment metadata
- Sailfish version and (when available) commit SHA
- .NET Runtime (framework description)
- OS, OS/Process architecture
- CPU model (best-effort)
- GC mode (Server/Workstation)
- JIT flags (TieredCompilation, QuickJit, QuickJitForLoops, OSR)
- Process priority and CPU affinity mask
- Timer source and effective resolution

### Environment health
- Overall Environment Health score (0–100)
- Summary label (Excellent/Good/Fair/Poor)

### Session info
- Timestamp (UTC)
- Session ID (unique per run)
- Randomization Seed (when seeded randomized run order is enabled)
- Tags (from Run Settings)
- CI system detection (e.g., GitHub Actions)

### Per‑method snapshots
For each executed test method, the manifest records:
- Display name
- Sample size (N) and number of warmup iterations
- Mean and StdDev
- CI margins (95% and 99%, best‑effort, based on N and StdDev)

## Where files are written

By default, Sailfish writes outputs to `sailfish_default_output`. You can override this via Run Settings or, when using the Test Adapter, via `.sailfish.json` using `GlobalSettings.ResultsDirectory`.

Examples:
- Programmatic: `RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory("./perf-results").Build()`
- Test Adapter: `.sailfish.json` → `{"GlobalSettings": {"ResultsDirectory": "SailfishIDETestOutput"}}`

## Markdown integration

When available, Sailfish includes a short "Reproducibility Summary" near the top of the consolidated Markdown output that calls out key environment details and points to the manifest file on disk.

## Example (truncated)

```json
{
  "SailfishVersion": "2.3.0",
  "DotNetRuntime": ".NET 9.0.7",
  "OS": "Microsoft Windows 11 Pro",
  "GCMode": "Server",
  "Jit": "Tiered=default; QuickJit=default; QuickJitForLoops=default; OSR=default",
  "EnvironmentHealthScore": 85,
  "SessionId": "20250115_120301-ab12cd34",
  "Methods": [
    { "TestCaseDisplayName": "MyTests.Foo()", "SampleSize": 100, "Mean": 1.2345, "StdDev": 0.0456, "CI95_MarginOfError": 0.0123 }
  ]
}
```

## Notes
- The manifest is written on a best‑effort basis; failures are logged at debug level and do not fail the test run.
- CPU model detection is best‑effort and may be null on some platforms.
- CI commit SHA is populated when common CI environment variables are present (e.g., `GITHUB_SHA`).

## See also
- [/docs/1/markdown-output](/docs/1/markdown-output)
- [/docs/1/environment-health](/docs/1/environment-health)

