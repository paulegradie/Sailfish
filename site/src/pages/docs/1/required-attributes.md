---
title: Required Attributes
---

## Sailfish Attribute

Apply the `[Sailfish]` attribute to the class.

```csharp
[Sailfish(
    SampleSize = 2,
    NumWarmupIterations = 1,
    DisableOverheadEstimation = false,
    Disabled = false)]
public class AMostBasicTest { ... }
```

#### SampleSize

Sets the number of times the SailfishMethod will be invoked. This number should be as high as you can tolerate. Larger sample size improves the quality of the result.

#### NumWarmupIterations

Sets the number of times the the SailfishMethod will be invoked before timing begins. This includes invocation of the SailfishIterationSetup and SailfishIterationTeardown lifecycle methods.

#### DisableOverheadEstimation

Sailfish performs overhead executions to estimate how much test result variance is due to the underlaying hardware. This will disable the feature, which will signifiantly increase test runtime. Estimation is performed before and after each test method is fully executed.

#### Disabled

Tests are discoverable but ignored when set to true.
---

## SailfishMethod Attribute

Apply the [SailfishMethod] attribute to a method you wish to time.

```csharp
[SailfishMethod]
public async Task SailfishMethod(CancellationToken ct) => ...
```
---

### Injecting Cancellation Tokens

Any Sailfish lifecycle method can request a `CancellationToken`, which will be injected by the framework.

### Sync vs Async

Any lifecycle method may be implemented as sync or async.
