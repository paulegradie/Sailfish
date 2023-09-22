---
title: The Sailfish Test Lifecycle
---

Sailfish allows you to implement multiple `SailfishMethods` in a single class, as well as multiples of (6) lifecycle methods for expressive control over your tests.

Test cases are built for each variable combination (if any) for each test method. Each test case is iterated `(int)SampleSize` times.

For each test case, the test lifecycle is as follows:

1. Test Class instantiation
1. GlobalSetup (once per class)
1. Method Setup
1. Iteration Setup
1. SailfishMethod
1. IterationTeardown (return to `SailfishIterationSetup` when SampleSize > 1)
1. MethodTeardown (return to `SailfishMethodSetup` when Method count > 1)
1. GlobalTeardown (once per class)

## Lifecycle Method Attributes

Sailfish exposes six (6) lifecycle attributes that give you fine-grain control within your test class. Below are methods that demonstrate how to use each lifecycle method:

### The Setup Phase

```csharp
[SailfishGlobalSetup]
public async Task GlobalSetup(CancellationToken ct) => ...
// called once per test class at the beginning of execution
```

```csharp
[SailfishMethodSetup]
public async Task MethodSetup(CancellationToken ct) => ...
// called once before each test method per variable set
```

```csharp
[SailfishIterationSetup]
public async Task IterationSetup(CancellationToken ct) => ...
// called once before each test method invocation
```
---
### The Teardown Phase

```csharp
[SailfishIterationTeardown]
public async Task IterationTeardown(CancellationToken ct) => ...
// called once after each test method invocation
```

```csharp
[SailfishMethodTeardown]
public async Task MethodTeardown(CancellationToken ct) => ...
// called once after each test method per variable set
```

```csharp
[SailfishGlobalTeardown]
public async Task GlobalTeardown(CancellationToken ct) => ...
// called once per test class - at the end of all execution
```
---
## Multiple Lifecycle methods

You may implement more than of any lifecycle method. The order of execution within a given class is not guaranteed, however methods implemented in base classes will always be executed before child class methods.

## Targeting Specific SailfishMethods

You can optionally provide a params array of method names to the iteration or method setup / teardown lifecycle methods to taget specific SailfishMethods. If no names are provided, the lifecycle method is applied to all methods.

```csharp
[SailfishMethodSetup(nameof(TestMethod))]  <-- method name
public async Task MethodSetup(CancellationToken ct) => ...

[SailfishMethod]
public void TestMethod() => ...

```
You may do this with:
- **SailfishMethodSetup**
- **SailfishMethodTeardown**
- **SailfishIterationSetup**
- **SailfishIterationTeardown**

## Property and Field Management

When multiple test cases are created for a class, distinct instances of the class are created. Properties and Fields that are set in the global lifecycle methods must therefore be cloned to new instances that do not execute the global lifecycle method.

The following modifiers are allowed when creating a property or field where the data is a set during lifecycle invocation:

### Properties

```csharp
public Type Public { get; set; }
protected Type Protected { get; set; }
```

### Fields

```csharp
internal Type InternalField;
protected Type ProtectedField;
private Type PrivateField;
public Type PublicField;
```
