---
title: The Sailfish Test Lifecycle
---

Sailfish allows you to implement multiple `SailfishMethods` in a single class, as well as multiple of (6)lifecycle methods for expressive control over your tests.

Test cases are built for each variable combination (if any) for each test method. Each test case is iterated `(int)SampleSize` times.

For each test case, the test lifecycle is as follows:

1. Test Class instantiation
1. GlobalSetup (once per class)
1. Method Setup
1. Iteration Setup
1. SailfishMethod
1. IterationTeardown (return to `SailfishIterationSetup` when SampleSize > 1)
1. MethodTeardown (return to `SailfishMethodSetup` when Method count > 1)

## Lifecycle Method Attributes

Sailfish exposes six (6) lifecycle attributes that give you fine-grain control within your test class. Below are methods that demonstrate how to use each lifecycle method:

### The setup phase

```csharp
[SailfishGlobalSetup]
public async Task GlobalSetup(CancellationToken ct)
{
  // This will be called once at the beginning
  // of all test cases produced from the test class
}
```

```csharp
[SailfishMethodSetup]
public async Task MethodSetup(CancellationToken ct)
{
    // This will be called once BEFORE
    // each method inside the test class
}
```

```csharp
[SailfishIterationSetup]
public async Task IterationSetup(CancellationToken ct)
{
    // This will be called once BEFORE each invocation of
    // a test method. So, if you have define SampleSize = 3
    // in your `Sailfish` class attribute, this will be
    // invoked before each of the 3 test method invocations
}
```

### The Teardown Phase

```csharp
[SailfishIterationTeardown]
public async Task IterationTeardown(CancellationToken ct)
{
    // This will be called once AFTER each invocation of
    // a test method. So, if you have define SampleSize = 3
    //  in your `Sailfish` class attribute, this will be
    // invoked after each of the 3 test method invocations
}
```

```csharp
[SailfishMethodTeardown]
public async Task ExecutionMethodTeardown(CancellationToken ct)
{
    // This will be called once AFTER each method
    // inside the test class
}
```

```csharp
[SailfishGlobalTeardown]
public async Task GlobalTeardown(CancellationToken ct)
{
    // This will be called once at the end of all
    // test cases produced from the test class
}
```
---

# Property and Field Management

When multiple test cases are created for a class, distinct instances of the class are created. Properties and Fields that are set in the global lifecycle methods must therefore be cloned to new instances that do not execute the global lifecycle method.

The following modifiers are allowed when creating a property or field where the data is a set during lifecycle invocation:

## Properties

```csharp
public Type Public { get; set; }
protected Type Protected { get; set; }
```

## Fields

```csharp
internal Type InternalField;
protected Type ProtectedField;
private Type PrivateField;
public Type PublicField;
```
