---
title: Release Notes
---

## Current Release Notes

{% callout title=".NET 10 support, .NET 8 dropped" type="success" %}
Sailfish, `Sailfish.TestAdapter`, and `Sailfish.Analyzers` now target `net9.0` and `net10.0` only. If you're on `net8.0`, stay on Sailfish 0.0.115 or upgrade your project.
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