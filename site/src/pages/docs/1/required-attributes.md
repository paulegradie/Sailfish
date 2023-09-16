---
title: Required Attributes
---
There are two required attributes to indicate a Sailfish Test:


## Sailfish Attribute

Apply the `[Sailfish]` attribute to the class.

```csharp
[Sailfish(SampleSize = 2, NumWarmupIterations = 1, DisableOverheadEstimation = false, Disabled = false)]
public class AMostBasicTest { ... }
```

### SampleSize

This property sets the number of times the main test method will be invoked. This number should be as high as you can tolerate. Larger sample size improves the reliablity of the result.

### NumWarmupIterations

This property sets the number of times the `SailfishIterationSetup`, main test method and `SailfishIterationTeardown` will be invoked (in that order) before beginning the timing process.

### DisableOverheadEstimation

Sailfish performs overhead executions to estimate how much test result variance is due to the underlaying hardware. This will disable the feature, which will signifiantly increase test runtime. Estimation is performed before and after each test method is fully executed.

### Disabled

Tests are discoverable but ignored when set to true.
---

## SailfishMethod Attribute

Apply the `[SailfishMethod]` attribute to a method you wish to time.

```csharp
[SailfishMethod]
public async Task SailfishMethod(CancellationToken cancellationToken)
{
    // This is where you place code you wish to be timed
}
```

### Injecting Cancellation Tokens

Any sailfish lifecycle method can request a `CancellationToken`, which will be injected by the framework.

### Sync vs Async

Any lifecycle method may be implemented as sync or async.
