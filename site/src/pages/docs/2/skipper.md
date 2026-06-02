---
title: Skipper (AI)
---

## Introduction

**Skipper** is Sailfish's optional **AI analysis layer** — the crewmate that reads the instruments (SailDiff and ScaleFish) and tells you, in plain language, **what changed and why**. Where SailDiff tells you a method got 18% slower and ScaleFish tells you it scales like O(n²), Skipper reads the *code under test*, explains the likely cause, and cites the exact `file:line`.

Skipper is **strictly additive and opt-in**. It ships with no model dependencies and no API keys. You bring your own agent — a one-shot completion, a local model, or a full agentic loop (e.g. the Claude Agent SDK / `claude` CLI) — by implementing a single interface. If you don't register one, Sailfish behaves exactly as before.

The guiding principle: **Sailfish owns the intelligence; you own the transport.** Sailfish assembles a *grounded* context packet from the authoritative SailDiff/ScaleFish numbers and your environment, and the model **reasons over those figures — it never recomputes or invents them**. For any claim about your code, it cites a real `file:line` it actually read.

## Enabling Skipper

Two steps: turn it on in the builder, and register an agent.

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithSailDiff()      // Skipper explains "why did this change?"
    .WithScaleFish()     // Skipper explains "why does this scale like that?"
    .WithAiAnalysis()    // turn on the Skipper layer
    .Build();
```

```csharp
// Register your agent from an IRegisterSailfishServices provider.
public class MyRegistration : IRegisterSailfishServices
{
    public Task RegisterAsync(IServiceCollection services, CancellationToken ct = default)
    {
        services.AddSingleton<ISailfishAgent, MyAgent>();
        return Task.CompletedTask;
    }
}
```

That's it. With no agent registered, `.WithAiAnalysis()` is a no-op — the feature stays completely invisible.

### Running Skipper from the IDE — the green "play" button

Sailfish benchmarks run as ordinary tests: install the **`Sailfish.TestAdapter`** package in your test project and every `[Sailfish]` class and `[SailfishMethod]` gets a gutter **"play" button** in Visual Studio, Rider, and VS Code, and runs under `dotnet test`. (If the play buttons don't appear — especially in Rider, which needs VSTest discovery switched on — see [Getting the gutter "play" buttons to appear](/docs/2/sailfish).)

There's no `RunSettingsBuilder` in this path — the adapter runs in its own process and never sees programmatic registration — so Skipper is configured by **file**, not code. Three things must be true before the play button will produce a Skipper verdict.

**1. SailDiff and/or ScaleFish must be enabled.** Skipper explains *their* output, so at least one has to produce a comparison. In the **`.sailfish.json`** at (or above) your test directory:

```jsonc
{
  "SailDiffSettings": { "Disabled": false },
  "ScaleFishSettings": {}
}
```

**2. Skipper must be turned on**, in that same `.sailfish.json`:

```jsonc
{
  "AiAnalysisSettings": { "Enabled": true }
}
```

**3. An agent must be registered** from an `IRegisterSailfishServices` provider in the test project — the adapter discovers it automatically, and it's the *only* registration seam the play-button path can see:

```csharp
public class RegistrationProvider : IRegisterSailfishServices
{
    public Task RegisterAsync(IServiceCollection services, CancellationToken ct = default)
    {
        services.AddSingleton<ISailfishAgent, ClaudeAgentModelProvider>();
        return Task.CompletedTask;
    }
}
```

For the reference `ClaudeAgentModelProvider`, the `claude` CLI must be installed and on your `PATH`. Without *any* registered agent, `Enabled: true` is a harmless no-op.

**The loop is run-twice.** SailDiff compares your latest run against the previous one, so a single run has nothing to diff:

1. Click the play button once — this records the **baseline** (no comparison yet).
2. Change your code.
3. Click play again — SailDiff produces the before/after, and Skipper explains it.

{% callout title="Run one test, not the whole suite" type="note" %}
The agent is invoked **once per analyzed comparison** — running the entire suite fans out into one call per test (and, for the reference agent, one `claude` invocation each). While you iterate, click the play button on a **single** benchmark. The Skipper verdict prints to the **test output window**, right beneath the SailDiff table; the `skipper-review_*.json` and `skipper-report_*.md` artifacts land in your results directory (`GlobalSettings.ResultsDirectory`).
{% /callout %}

A complete, copy-pasteable `.sailfish.json` — this is the one the repo's `PerformanceTests` project ships:

```jsonc
{
  "SailDiffSettings": { "TestType": "TwoSampleWilcoxonSignedRankTest", "Alpha": 0.005, "Disabled": false },
  "ScaleFishSettings": {},
  "AiAnalysisSettings": { "Enabled": true },
  "GlobalSettings": { "ResultsDirectory": "SailfishIDETestOutput", "Round": 5 }
}
```

The optional `AiAnalysisSettings` keys mirror the programmatic settings: `WriteReviewArtifact`, `EmitConsoleSummary`, `UseResponseCache` (all default `true`). `Role` is programmatic-only — the test-adapter path always runs the default `Explain` role.

### Working examples in the repo

A complete, runnable reference ships in the repo. `ClaudeAgentModelProvider` (a reference agent that drives the local `claude` CLI with read-only tools) is registered two ways:

- **Test Adapter path** — the `PerformanceTests` project registers it in its `RegistrationProvider` and enables Skipper via `.sailfish.json` (the exact setup shown above). Click the play button on a single benchmark to see it.
- **Programmatic path** — `ConsoleAppDemo` reuses the same provider and enables Skipper with `.WithAiAnalysis()`.

## The one interface you implement

```csharp
public interface ISailfishAgent
{
    Task<SkipperReview> RunAsync(SkipperSession session, CancellationToken cancellationToken);
}
```

`SkipperSession` carries everything your agent needs: the role, the grounded `PerformanceNarrativeContext` (the authoritative numbers), the **capabilities** it has been granted (locally, read-only code access scoped to your repository), and the repository root. Your implementation simply forwards this to a model and returns a `SkipperReview`.

A `SkipperReview` is structured, not just prose:

- **`OverallVerdict`** — `Improved` / `Regressed` / `NotSignificant` / `Inconclusive`.
- **`Findings`** — per-test diagnoses, each with its own verdict, a summary, and the `file:line` locations it cited.
- **`ConsoleSummary`** and **`MarkdownReport`** — the terse and the deep renderings.

A reference **agentic** implementation that drives the `claude` CLI (read-only `Read`/`Grep`/`Glob` scoped to the repo) ships in the repo as `ClaudeAgentModelProvider`, in the `PerformanceTests/Skipper` folder (`ConsoleAppDemo` reuses it). Copy it and swap the transport to taste.

## What Skipper produces

**Inline in the console**, beneath the SailDiff table:

```
🧭 Skipper  🔴 REGRESSED
ParseHeaders is 18% slower than baseline and it's a real change (p<0.001, CV 2.1%).
  • 🔴 Bench.ParseHeaders — regex compiled inside the per-row loop
       ↳ Parser.cs:88
```

Chips: 🔴 regressed · 🟢 improved · ⚪ not significant · 🟡 inconclusive. The verdict vocabulary matches SailDiff (a comparison is *not significant*, never "no change").

**On disk**, beside your run output:

| File | What it is |
| ---- | ---------- |
| `skipper-review_<timestamp>_<kind>.json` | The structured review — machine-readable, for a CI bot or orchestrator to consume. |
| `skipper-report_<timestamp>_<kind>.md` | The deep human-readable write-up: call path, cited code, suggested fix. |

`<kind>` is `saildiff` or `scalefish`, so a run that does both never overwrites itself.

## Reliability-aware verdicts

Skipper's context packet includes an **environment snapshot** drawn from Sailfish's reproducibility manifest and environment health check — runtime, OS, CPU, GC mode, JIT, CPU affinity, timer, plus any health concerns. This lets Skipper *temper* its verdict on a noisy or misconfigured host, which is the dominant failure mode of microbenchmarking:

> *"This 12% 'regression' is low-confidence: CV was 8.4% and the power plan is 'Balanced'. Re-run on a quiet, fixed-clock host before trusting it."*

Each comparison also carries its effect size and the **minimum detectable effect** — so Skipper can tell you when a run was simply underpowered to catch the change you care about.

## Two questions Skipper answers

- **"Why did this change?"** — from SailDiff. Skipper reads the implicated method, follows it into the system under test, and explains the cause (an allocation in a loop, an N+1 query, a lost fast-path) with citations.
- **"Why does this scale like that, and what happens at 10× the data?"** — from ScaleFish. Skipper takes the best-fit complexity class (and whether it's statistically distinguishable from the runner-up) and projects the fitted curve to larger N: *"O(n²), R²=0.98 — at 10,000 items expect ~500ms."*

## Workflow: rerun in place

The most natural local loop is the simplest one. Sailfish's tracking files capture each run, and SailDiff automatically compares your **latest run against the previous one** — so you just:

1. Run your benchmarks.
2. Change your code.
3. Run again.

SailDiff produces the before/after, and Skipper explains it — no file paths to type, nothing to wire up. (You can still point SailDiff at specific prior tracking files when you want a fixed baseline; rerun-in-place is simply the zero-friction default.)

## Settings

`AiAnalysisSettings` (pass via `WithAiAnalysis(settings)`) controls the layer:

| Setting | Default | Effect |
| ------- | ------- | ------ |
| `WriteReviewArtifact` | `true` | Write `review.json` + `report.md` beside the run output. |
| `EmitConsoleSummary` | `true` | Print the inline verdict block beneath the table. |
| `UseResponseCache` | `true` | Reuse a cached review for an identical context — no re-spend, and stable, reproducible output. |
| `Role` | `Explain` | The authority the agent runs under. (`Review`/`Remediate`/`Author` are reserved for future CI and automation roles.) |

## Privacy & safety

- **Nothing leaves your machine** unless *your* agent sends it. Sailfish only assembles the context and calls your `ISailfishAgent`.
- Skipper runs **after** your numbers are computed and printed, and **never throws into a run** — if the agent is missing, offline, or errors, your benchmark output is completely unaffected.
- The reference agent grants **read-only** code access; Skipper proposes, it does not act.
