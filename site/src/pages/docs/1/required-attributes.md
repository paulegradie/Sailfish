---
title: Required Attributes
---

Every Sailfish performance test requires two essential attributes to function properly. These attributes define the test class and specify which methods to measure.

{% info-callout title="Essential Setup" %}
The `[Sailfish]` and `[SailfishMethod]` attributes are the minimum requirements for any performance test. Everything else is optional configuration.
{% /info-callout %}

## 🏷️ Sailfish Attribute

{% success-callout title="Class-Level Configuration" %}
Apply the `[Sailfish]` attribute to your test class to mark it for performance testing and configure execution parameters.
{% /success-callout %}

```csharp
[Sailfish(
    SampleSize = 45,
    NumWarmupIterations = 2,
    DisableOverheadEstimation = false,
    Disabled = false)]
public class AMostBasicTest { ... }
```

### ⚙️ Configuration Properties

{% feature-grid columns=2 %}
{% feature-card title="SampleSize" description="Number of times the SailfishMethod will be invoked. Higher values improve result quality." /%}

{% feature-card title="NumWarmupIterations" description="Number of warmup invocations before timing begins, including setup/teardown methods." /%}

{% feature-card title="DisableOverheadEstimation" description="Disables overhead estimation to reduce test runtime (not recommended)." /%}

{% feature-card title="Disabled" description="Makes tests discoverable but ignored during execution." /%}
{% /feature-grid %}

{% tip-callout title="Sample Size Recommendations" %}
**SampleSize** should be as high as you can tolerate. Larger sample sizes improve statistical significance and result reliability. Start with 30-50 samples for most scenarios.
{% /tip-callout %}

{% warning-callout title="Overhead Estimation" %}
**DisableOverheadEstimation** should generally remain `false`. Sailfish performs overhead executions to estimate measurement variance, providing more accurate results.
{% /warning-callout %}

## 🎯 SailfishMethod Attribute

{% code-callout title="Method-Level Measurement" %}
Apply the `[SailfishMethod]` attribute to any method you wish to measure. This is where the actual performance timing occurs.
{% /code-callout %}

```csharp
[SailfishMethod]
public async Task SailfishMethod(CancellationToken ct) => ...
```

### 🔧 Method Implementation Guidelines

{% feature-grid columns=2 %}
{% feature-card title="Cancellation Token Support" description="Any Sailfish lifecycle method can request a CancellationToken for proper async cancellation handling." /%}

{% feature-card title="Sync vs Async" description="Any lifecycle method may be implemented as synchronous or asynchronous based on your needs." /%}
{% /feature-grid %}

{% note-callout title="Best Practices" %}
- Use `CancellationToken` parameters for proper async operation cancellation
- Keep your `[SailfishMethod]` focused on the code you want to measure
- Avoid heavy setup/teardown logic in the measured method - use lifecycle methods instead
{% /note-callout %}
