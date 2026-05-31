---
title: Release Notes
---

## Current Release Notes

{% callout title="Breaking: DI container is now Microsoft.Extensions.DependencyInjection" type="warning" %}
Sailfish has moved off Autofac onto the standard .NET DI abstraction (`IServiceCollection` / `IServiceProvider`). Implement `IRegisterSailfishServices` in place of `IProvideARegistrationCallback`, and use `IServiceCollection.AddSailfish(runSettings)` in place of `ContainerBuilder.RegisterSailfishTypes(runSettings)`. The `Autofac` package is no longer a dependency. [Migration guide →](/docs/1/test-dependencies)
{% /callout %}

{% callout title=".NET 10 support, .NET 8 dropped" type="success" %}
Sailfish, `Sailfish.TestAdapter`, and `Sailfish.Analyzers` now target `net9.0` and `net10.0` only. If you're on `net8.0`, stay on Sailfish 0.0.115 or upgrade your project.
{% /callout %}

{% callout title="Behavior change: OperationsPerInvoke reports per-operation time" type="warning" %}
When `OperationsPerInvoke` > 1 (set explicitly, or chosen by `TargetIterationDurationMs` auto-tuning), Sailfish now divides each measured iteration by the batch size, so reported mean/median/etc. represent the cost of a **single operation** and stay comparable across methods and runs regardless of the batch size. Previously the per-iteration aggregate was reported, which inflated results by the OPI factor. If you use OPI > 1, expect reported durations to drop accordingly — they now reflect true per-op cost. If you persist tracking data for SailDiff history on OPI > 1 tests, baselines recorded before this change will read as a one-time drop; re-baseline after upgrading. [Learn more →](/docs/1/iteration-tuning)
{% /callout %}

{% callout title="Performance: compiled-delegate invocation" type="success" %}
The timed method is now invoked through a compiled, allocation-free delegate instead of per-call reflection. On .NET 10 this drops per-invocation harness overhead from ~40 ns to ~1.5 ns and eliminates per-call allocations (272 B → 0 B); the overhead baseline is measured with a structurally identical idle delegate so the subtraction cancels almost exactly. The result is a lower, more stable noise floor, so Sailfish resolves smaller differences. Behavior note: a method returning `Task`/`ValueTask` *without* the `async` keyword is now correctly awaited and timed (previously under-measured). [Learn more →](/docs/1/measurement-and-overhead)
{% /callout %}

{% callout title="Feature: Configuration Recipes via SailfishPreset" type="note" %}
`RunSettingsBuilder.WithPreset(SailfishPreset.Default | Tight | Relaxed)` seeds adaptive sampling, outlier handling, and SailDiff defaults from a single call. [Learn more →](/docs/1/presets)
{% /callout %}

{% callout title="Feature: RunOnce on method setup/teardown" type="note" %}
`[SailfishMethodSetup(nameof(Foo), nameof(Bar), RunOnce = true)]` invokes a setup/teardown at most once per executor run, even when multiple `MethodNames` are listed. [Learn more →](/docs/1/sailfish-test-lifecycle)
{% /callout %}

{% callout title="Fix: Method-comparison failure handling" type="note" %}
Failed members of a comparison group now publish `TestOutcome.Failed` (no more silent `TestOutcome.None`) and don't block sibling members from completing the comparison batch. [Learn more →](/docs/1/method-comparisons)
{% /callout %}

{% callout title="Feature highlight: Timer Calibration + Jitter Score" type="note" %}
Captures timer resolution and baseline overhead, computes a 0–100 Jitter Score from dispersion, and surfaces it in Markdown, the manifest, and Environment Health (Timer Jitter). [Learn more →](/docs/1/markdown-output)
{% /callout %}

{% callout title="Feature highlight: Precision/Time Budget Controller" type="note" %}
Helps tests finish within per-method time budgets by conservatively relaxing precision targets when enabled. [Learn more →](/docs/1/precision-time-budget)
{% /callout %}

{% callout title="Feature highlight: Environment Health Check" type="note" %}
Validates your benchmarking environment and appends a health summary to each test’s Output window.
[Learn more →](/docs/1/environment-health)
{% /callout %}


Release notes have been moved to [GitHub Releases](https://github.com/paulegradie/Sailfish/releases) for better integration with the development workflow.

**[📋 View All Releases →](https://github.com/paulegradie/Sailfish/releases)**

### Why the Move?

- **Real-time updates**: Release notes are now automatically generated when new versions are published
- **Better GitHub integration**: Releases are prominently displayed on the repository page
- **Automatic linking**: Direct links to commits, pull requests, and contributors
- **Asset management**: NuGet packages are directly attached to each release
- **Notifications**: Subscribe to release notifications on GitHub