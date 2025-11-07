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
- Process Priority (recommend AboveNormal/High)
- GC Mode (recommend Server GC)
- CPU Affinity (recommend pinning to 1 core for microbenchmarks)
- High‑Resolution Timer availability
- Power Plan (recommend High Performance)
- Background CPU load (process)

Each check contributes to the health score and may include a brief recommendation.

## Where to see it
- Run-level message: printed near the start of the run in your standard INF/DBG logs.
- Per-test Output window: appended to the bottom of every test’s output alongside Sailfish’s performance tables (new behavior).

Example excerpt:

```
Sailfish Environment Health: 87/100 (Excellent)
 - Process Priority: Warn (Normal) — Consider High or AboveNormal to reduce scheduler noise
 - GC Mode: Pass (Server GC enabled)
 - CPU Affinity: Warn (All cores) — Pin to 1 core to minimize cross-core jitter
 - Timer: Pass (High‑resolution timer)
 - Power Plan: Pass (High performance)
 - Background CPU (process): Pass (1%)
```

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

