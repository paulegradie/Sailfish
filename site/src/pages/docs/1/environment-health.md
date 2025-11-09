---
title: Environment Health Check
---

The Environment Health Check validates your test host to reduce scheduler noise and improve result stability. It runs automatically at the start of each test run and surfaces clear guidance if the host isn’t ideal for benchmarking.

{% callout title="TL;DR" type="note" %}
- Runs automatically at test run start (default: enabled)
- Shows a 0–100 score with a label (Excellent / Good / Fair / Poor)
- Appears in two places:
  - In the INF/DBG log stream near run start
  - Appended to the bottom of each test’s Output window (new)
- Disable globally with `RunSettingsBuilder.WithEnvironmentHealthCheck(false)`
{% /callout %}

## Why it exists
Microbenchmark results are sensitive to OS scheduling, GC settings, and power plans. The health check quickly tells you whether your environment is suitable and offers actionable recommendations to improve stability.

## What it checks
The current set of checks includes:
- Build Mode (warns in Debug; recommend Release optimizations)
- JIT (Tiered/OSR) — reports TieredCompilation, QuickJit, QuickJitForLoops, On-Stack Replacement flags; recommend enabling Tiered JIT for representative steady-state performance
- Process Priority (recommend AboveNormal/High)
- GC Mode (recommend Server GC)
- CPU Affinity (recommend pinning to 1 core for microbenchmarks)
- High‑Resolution Timer availability + Sleep/Delay granularity check
- Power Plan (recommend High Performance)
- Background CPU load (process)

Each check contributes to the health score and may include a brief recommendation.

## Where to see it
- Run-level message: printed near the start of the run in your standard INF/DBG logs.
- Per-test Output window: appended to the bottom of every test’s output alongside Sailfish’s performance tables (new behavior).
- Consolidated session markdown: included as a "🏥 Environment Health Check" section near the top of the session file with score and top entries.

Example excerpt:

```
Sailfish Environment Health: 87/100 (Excellent)
 - Build Mode: Warn (Debug) — Use Release (optimized) for stable measurements
 - JIT (Tiered/OSR): Pass (Tiered=default; QuickJit=default; QuickJitForLoops=default; OSR=default)
 - Process Priority: Warn (Normal) — Consider High or AboveNormal to reduce scheduler noise
 - GC Mode: Pass (Server GC enabled)
 - CPU Affinity: Warn (All cores) — Pin to 1 core to minimize cross-core jitter
 - Timer: Pass (High‑resolution timer; Sleep(1) median ≈ 15.6 ms)
 - Power Plan: Pass (High performance)
 - Background CPU (process): Pass (1%)
```
## Timer granularity and short sleeps

On many systems, very short sleeps/await delays are quantized to the OS scheduler tick. Examples:

- Windows (default): ~15.6 ms tick — Sleep(10) often measures ~15–16 ms
- Linux: commonly ~1-3 ms depending on kernel HZ and power settings
- macOS: often ~1-2 ms, but can vary with power and coalescing

Sailfish’s health check now reports both:
- The high-resolution performance counter resolution (Stopwatch)
- The effective sleep granularity (median of Thread.Sleep(1))

Guidance:
- For targets below the scheduler tick, prefer CPU-bound waits (busy-wait using Stopwatch) or use durations well above the tick.
- We do not subtract scheduler latency from your measurements because it reflects real-world behavior when your code uses Sleep/Delay.



## Timer Jitter (from Timer Calibration)

When Timer Calibration is enabled (default), the health report includes a Timer Jitter entry based on the dispersion (RSD%) of no‑op timing samples. This yields a 0–100 Jitter Score (higher is better): `score = clamp(0, 100, 100 − 4 × RSD%)`.

Thresholds:
- Pass: RSD% ≤ 5%
- Warn: 5% < RSD% ≤ 15%
- Fail: RSD% > 15%

Toggle (global):
```csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithTimerCalibration(true) // default: true; set to false to disable
    .Build();
```

Also appears in:
- Consolidated Markdown header as a “Timer Calibration” section
- Reproducibility Manifest (TimerCalibration snapshot)

See also: [/docs/1/markdown-output](/docs/1/markdown-output), [/docs/1/reproducibility-manifest](/docs/1/reproducibility-manifest)

## How it works
- The Test Adapter runs the health check once at test run start and stores the report for the session.
- The summary is:
  - Emitted to the framework log stream (run-level message)
  - Appended to each test’s Output window message when results are published
- No test behavior changes—this is purely diagnostic output.

## Configuration
Enabled by default. You can disable it globally for a run:

```csharp
var runSettings = RunSettingsBuilder.CreateBuilder()
    .WithEnvironmentHealthCheck(false)
    .Build();
```

## When to use (guidance)
Keep it enabled for CI and for local baseline/verification runs to catch unstable environments. For quick exploratory runs where output brevity matters, you can disable it temporarily.

{% callout title="IDE tip (Rider/VS)" type="note" %}
- Run-level messages appear under the session/root node’s Output/Messages.
- Per-test summaries are appended to each test’s Output window.
- In Rider you may need to enable “VS Test Adapter Support” to see custom adapter output.
{% /callout %}



## Related

- Reproducibility Manifest: [/docs/1/reproducibility-manifest](/docs/1/reproducibility-manifest)
