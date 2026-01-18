---
title: 'Welcome!'
---

Sailfish is a .NET library that you can use to write performance tests that are simple, consistent & familiar.

This library was built to support and inspire the incorporation of performance testing into test driven development as well as the creation of production ready performance monitoring systems.



## Status

[![Build Pipeline v3.0](https://github.com/paulegradie/Sailfish/actions/workflows/build-v3.0.yml/badge.svg)](https://github.com/paulegradie/Sailfish/actions/workflows/build-v3.0.yml)
![Nuget](https://img.shields.io/nuget/dt/Sailfish)
[![codecov](https://codecov.io/gh/paulegradie/Sailfish/graph/badge.svg?token=UN17VRVD0N)](https://codecov.io/gh/paulegradie/Sailfish)

## What's New

{% callout title="New: Environment Health Check" type="note" %}
Quickly validates your benchmarking environment and emits a 0–100 health score with actionable recommendations. Now appears in INF/DBG logs and appended to each test’s Output window.
[Learn more →](/docs/1/environment-health)
{% /callout %}


{% callout title="New: Adaptive Sampling" type="note" %}
Sailfish can now automatically stop sampling when results are statistically stable. Cut CI time while preserving rigor.
[Learn more →](/docs/1/adaptive-sampling)
{% /callout %}

{% quick-links %}
{% quick-link title="Getting Started" description="Install and run your first test" icon="book-open" href="/docs/0/getting-started" /%}
{% quick-link title="Quick Start" description="Copy-paste minimal setup" icon="zap" href="/docs/0/quick-start" /%}
{% quick-link title="Outputs" description="Markdown & CSV format guides" icon="document-text" href="/docs/1/output-attributes" /%}
{% quick-link title="Method Comparisons" description="Group methods and compare statistically" icon="chart-bar" href="/docs/1/method-comparisons" /%}
{% quick-link title="Adaptive Sampling" description="Stop when results are stable—faster CI with statistical rigor" icon="lightbulb" href="/docs/1/adaptive-sampling" /%}
{% /quick-links %}


## Why Sailfish

{% quick-links %}
{% quick-link title="Statistical Rigor" description="Confidence intervals, outlier handling, adaptive sampling" icon="chart-bar" href="/docs/1/confidence-intervals" /%}
{% quick-link title="Extensible" description="Plug in providers and extend behaviors for your needs" icon="plugins" href="/docs/3/extensibility" /%}
{% quick-link title="Production Friendly" description="Outputs you can ship: Markdown and CSV for pipelines" icon="document-text" href="/docs/1/markdown-output" /%}
{% /quick-links %}
