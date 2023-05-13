---
title: The Sailfish Attribute
---

To mark a Sailfish test class, you need to apply the `[Sailfish]` attribute to the class.

```csharp
[Sailfish]
public class AMostBasicTest {}
```

The attribute hold three (3) properties:

- NumIterations
- NumWarmupIterations
- Disable

```csharp
[Sailfish(NumIterations = 3, NumWarmupIterations = 3, Disabled = false)]
public class AMostBasicTest {}
```

## NumIterations

This property sets the number of times the main test method will be invoked. This number should be as high as you can tolerate, since the more data you collect, the higher confidence you will have on its true value. This will be important to mitigate the effects of variance in your system.

## NumWarmupIterations

This property sets the number of times the `SailfishIterationSetup`, main test method and `SailfishIterationTeardown` will be invoked (in that order) before beginning the timing process.

## Disabled

If this is false, it will un-discoverable in the console app setting. Tests are not disabled when running in the IDE.
