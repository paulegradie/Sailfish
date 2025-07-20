---
title: Essential Information
---

Understanding these key concepts will help you write better performance tests and interpret results correctly.

## 🎯 Core Principles

{% warning-callout title="Important Behavior" %}
Sailfish follows specific execution patterns that are crucial to understand for accurate performance testing.
{% /warning-callout %}

### 🔄 Tests are Not Parallelized

{% info-callout title="Sequential Execution" %}
All Sailfish tests run sequentially, one after another. This ensures consistent resource usage and prevents interference between tests that could skew performance measurements.
{% /info-callout %}

**Why this matters:**
- Consistent CPU and memory usage patterns
- No resource contention between tests
- Reproducible results across test runs
- Accurate baseline measurements

### 📋 Run Order is Deterministic

{% info-callout title="Predictable Execution" %}
Tests always execute in the same order, ensuring reproducible results and making it easier to identify performance regressions.
{% /info-callout %}

**Benefits:**
- Consistent warm-up patterns
- Predictable resource allocation
- Easier debugging of performance issues
- Reliable before/after comparisons

### ⚡ Tests Run In-Process

{% info-callout title="Same Process Execution" %}
Sailfish runs tests within the same process as the test runner, avoiding the overhead of process creation and inter-process communication.
{% /info-callout %}

Sailfish applies outlier detection and overhead estimation to run results. It does not perform any optimizations that would result in the tests needing to be run out-of-process.

**Advantages:**
- Lower measurement overhead
- More accurate timing for fast operations
- Simplified debugging and profiling
- Better integration with existing test infrastructure

## 📊 Statistical Analysis

{% tip-callout title="Built-in Intelligence" %}
Sailfish automatically applies statistical methods to ensure your performance measurements are reliable and meaningful.
{% /tip-callout %}

### 🎯 Outlier Detection

{% success-callout title="Automatic Quality Control" %}
Sailfish automatically identifies and handles outliers in your performance data using proven statistical methods.
{% /success-callout %}

- **Statistical Methods**: Uses proven algorithms to detect anomalous measurements
- **Automatic Filtering**: Removes outliers that could skew your results
- **Transparent Reporting**: Shows which measurements were considered outliers

### 🔧 Overhead Estimation

{% code-callout title="Precision Engineering" %}
The framework accounts for its own measurement overhead to provide the most accurate results possible.
{% /code-callout %}

- **Baseline Measurement**: Determines the cost of the measurement infrastructure
- **Automatic Compensation**: Subtracts overhead from final results
- **Accuracy Improvement**: Provides more precise measurements, especially for fast operations

## 💡 Best Practices

{% feature-grid columns=2 %}
{% feature-card title="Warm-up Considerations" description="Account for JIT compilation and other warm-up effects in your test design." /%}

{% feature-card title="Resource Management" description="Properly dispose of resources and avoid memory leaks that could affect subsequent tests." /%}

{% feature-card title="Consistent Environment" description="Run tests in consistent environments to ensure reproducible results." /%}

{% feature-card title="Meaningful Iterations" description="Use appropriate sample sizes for statistical significance." /%}
{% /feature-grid %}

{% note-callout title="The Bottom Line" %}
These execution characteristics make Sailfish particularly well-suited for micro-benchmarks and detailed performance analysis where measurement accuracy is critical.
{% /note-callout %}
