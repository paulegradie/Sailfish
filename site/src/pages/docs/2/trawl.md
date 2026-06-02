---
title: Trawl (Load Testing)
---

## Introduction

**Trawl** is Sailfish's **load-testing** mode. Where a `[SailfishMethod]` micro-benchmarks a method by running it sequentially many times, a `[Trawl]` method is invoked **concurrently by many virtual users for a sustained duration** — and Sailfish reports the things load tests care about: **throughput, latency percentiles (p50/p90/p95/p99), and error rate**.

A trawler drags a heavy net through the water under sustained load; that is exactly what this mode does to your system under test. It sits alongside the rest of the family — **SailDiff** (is this change a real regression?), **ScaleFish** (how does it scale?), and **Skipper** (why?) — and is designed to reuse them: the same statistical rigor that tells you a benchmark regressed will tell you a load profile regressed, and the same curve-fitting that classifies algorithmic complexity will find the **saturation knee** of a service.

> **Design principle — thin engine, deep analysis.** Sailfish does not aim to out-generate dedicated load generators. Its edge is turning load numbers into *trustworthy, explained, regression-gating* answers — the quadrant every existing tool leaves empty. Trawl is the minimum concurrent-load engine; the value compounds with SailDiff, ScaleFish, and Skipper.

## Authoring a load scenario

A Trawl scenario is just a method in a normal `[Sailfish]` class, marked with `[Trawl]` instead of `[SailfishMethod]`:

```csharp
[Sailfish]
public class CheckoutLoad
{
    private HttpClient client = null!;

    [SailfishGlobalSetup]
    public void Setup() => client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

    [Trawl(VirtualUsers = 50, DurationSeconds = 120, WarmupSeconds = 15)]
    public async Task Checkout(CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/checkout", Payload, ct);
        response.EnsureSuccessStatusCode();
    }
}
```

The scenario method is protocol-agnostic — it is any `async` method. Sailfish does not care whether it makes an HTTP call, a database query, or a gRPC request; it only measures how the method behaves under concurrency.

### Lifecycle and thread-safety

The enclosing class is an ordinary `[Sailfish]` class, so the usual hooks apply — warm a shared `HttpClient` or seed data in `[SailfishGlobalSetup]`. Because **all virtual users share the one test instance**, any scenario state must be thread-safe (a shared `HttpClient` is; a mutable field updated per request is not).

Trawl scenarios should also be **`async` and non-blocking**. In the closed model each virtual user is an independent task, so a scenario that blocks a thread synchronously (rather than `await`-ing) ties up a thread-pool thread for the whole run; at high `VirtualUsers` counts that can starve the pool and distort the ramp. Prefer `await`-ing genuinely asynchronous work.

### A method is one mode or the other

A method is either a microbenchmark (`[SailfishMethod]`) or a load scenario (`[Trawl]`) — never both. The **SF1022** analyzer enforces this at build time.

## The `[Trawl]` attribute

| Property | Default | Meaning |
|---|---|---|
| `VirtualUsers` | `10` | Concurrent virtual users (closed model). |
| `DurationSeconds` | `30` | Sustained, measured load duration (after warmup). |
| `WarmupSeconds` | `5` | Warmup duration; traffic is generated but not measured. |
| `Model` | `ClosedModel` | `ClosedModel` (fixed VUs) or `OpenModel` (target arrival rate). |
| `TargetRequestsPerSecond` | `0` | Target rate for the open model. |
| `Disabled` | `false` | Skip this scenario. |

## Run-wide settings (`.sailfish.json`)

The per-scenario attribute authors the scenario; run-wide **`TrawlSettings`** lets you reshape every scenario at run time without editing the test source — most usefully to shrink a load run in CI:

```jsonc
{
  "TrawlSettings": {
    "Disabled": false,            // global kill switch for all [Trawl] scenarios
    "VirtualUsersOverride": 5,    // clamp every closed-model scenario to 5 VUs
    "MaxDurationSecondsOverride": 10, // cap sustained duration at 10s
    "WarmupSecondsOverride": 2
  }
}
```

Every override is absent (`null`) by default, meaning "use the per-scenario attribute value". The same settings are available programmatically:

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithTrawlVirtualUsers(5)
    .WithTrawlMaxDuration(10)
    .Build();
```

## What you get today

Run a `[Trawl]` scenario (via `dotnet test`, the IDE, or the programmatic runner) and Sailfish executes the **closed model**: the configured number of virtual users hammer the scenario concurrently for the duration, after an unmeasured warmup. Each successful request's latency is recorded; failures are counted toward the error rate. Sailfish reports:

- **Throughput** (requests/second) and **error rate**
- **Latency percentiles** — p50/p90/p95/p99 and max — computed from the *full* sample set with **no outlier removal** (the slow tail is the point of a load test)

The latency distribution flows through Sailfish's normal output, so a load case shows up like any other test case. A scenario with zero successful requests fails the case.

## Closed vs open model

By default a scenario runs the **closed model**: `VirtualUsers` concurrent users, each looping as fast as the system allows. Throughput is emergent.

Set `Model = LoadModel.OpenModel` with a `TargetRequestsPerSecond` to run the **open model**: requests are dispatched at the target arrival rate regardless of how many are already in flight (`VirtualUsers` then caps concurrent in-flight requests — think connection-pool size). The open model is what exposes a system that can't keep up, and it applies **coordinated-omission correction**: latency is measured from each request's *intended* send time, so a stall is counted as latency on the requests that "should" have been sent during it — rather than being silently omitted (an overloaded system otherwise looks deceptively healthy).

```csharp
[Trawl(Model = LoadModel.OpenModel, TargetRequestsPerSecond = 500, VirtualUsers = 64, DurationSeconds = 120)]
public async Task Checkout(CancellationToken ct) { /* ... */ }
```

## Reports & artifacts

Each scenario prints a report — a summary line, a latency-percentile table, a latency **distribution plot** (honoring your configured `DistributionPlotStyle`), and Unicode **sparklines** of throughput and p99 over time. The same report is written to `<output>/trawl/<scenario>_<timestamp>.md`, and a machine-readable `…​.json` record (summary + a capped latency sample + the per-second time-series) is written alongside it — that JSON is what later regression analysis reads as a baseline.

## Regression gating

Every run is compared against its **most recent prior run** (the baseline persisted under `<output>/trawl/`) using SailDiff's statistical machinery — the exact same significance test the microbenchmark path uses, applied to the two latency distributions (outlier removal off, so the slow tail counts). The verdict reads either `Current is N% slower/faster than baseline …` or `NOT SIGNIFICANT`.

Turn it into a **CI gate** with `FailOnRegression` — a scenario that regressed significantly then fails its test case (non-zero `dotnet test`):

```jsonc
{ "TrawlSettings": { "FailOnRegression": true } }
```

## Status

Trawl is being delivered in phases. Shipping now: the public surface (`[Trawl]`, `TrawlSettings`, `TrawlResult`, SF1022), the **closed-model engine**, the **open arrival-rate model with coordinated-omission correction**, **reporting** (console + Markdown report, distribution plot, time-series sparklines, JSON persistence), and **SailDiff regression gating** (`FailOnRegression`). Still landing in subsequent releases: multi-stage load profiles (ramp/step), streaming histograms for long soaks, and ScaleFish saturation analysis.
