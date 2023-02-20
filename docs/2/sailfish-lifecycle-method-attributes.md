# Sailfish Lifecycle Method Attributes

Sailfish lifecycle methods provide flexibility in the setup and teardown phases of your performance tests.

Consider the following scenario:

You are interested in monitoring how long it takes to create a resource via your API, so you've written a Sailfish test that creates the resource. You've set `NumIterations` on your `Sailfish` class attribute, so your test method is going to be invoked 3 times. You know that you want to track response time when creating the resource, but you don't want to track how long it takes to delete the resource, so you can't put that behavior inside your test method.

This is the problem that lifecycle methods solve.


## Lifecycle Method Attributes

Sailfish exposes six (6) lifecycle attributes that give you fine-grain control within your test class. Below are methods that demonstrate how to use each lifecycle method:

### The setup phase
```csharp
[SailfishGlobalSetup]
public async Task GlobalSetup(CancellationToken cancellationToken)
{
  // This will be called once at the beginning of all test cases produced from the test class
}
```

```csharp
[SailfishMethodSetup]
public async Task MethodSetup(CancellationToken cancellationToken)
{
    // This will be called once BEFORE each method inside the test class
}
```

```csharp
[SailfishIterationSetup]
public async Task IterationSetup(CancellationToken cancellationToken)
{
    // This will be called once BEFORE each invocation of a test method.
    // So, if you have define NumIterations = 3 in your `Sailfish` class attribute,
    // this will be invoked before each of the 3 test method invocations
}
```
### The Teardown Phase
```csharp
[SailfishIterationTeardown]
public async Task IterationTeardown(CancellationToken cancellationToken)
{
    // This will be called once AFTER each invocation of a test method.
    // So, if you have define NumIterations = 3 in your `Sailfish` class attribute,
    // this will be invoked after each of the 3 test method invocations
}
```
```csharp
[SailfishMethodTeardown]
public async Task ExecutionMethodTeardown(CancellationToken cancellationToken)
{
    // This will be called once AFTER each method inside the test class
}
```
```csharp
[SailfishGlobalTeardown]
public async Task GlobalTeardown(CancellationToken cancellationToken)
{
    // This will be called once at the end of all test cases produced from the test class
}
```

## Injecting Cancellation Tokens

When you request a cancellation token in any lifecycle method, it will be injected by the test runner. If you do not request it, it will not be injected, and the test will not error. Other registered dependencies are not allowed to be injected in your tests, in order to help you fall in to the 'pit of success' when writing clean, maintainable, understandable tests. Sailfish is all about flexibility, but method level dependnecy injection crosses the line. :pray:

## Sychronous vs Asynchronous

You may have noticed that these lifecycle methods are asynchronous (i.e. they are defined as `public async Task MethodName(CancellationToken ct)`).

If you do not need to execute code asynchronously, you have the option of defining these methods synchronously as well. For example, the following is a valid global setup method:


```csharp
[SailfishGlobalSetup]
public void GlobalSetup()
{
    // do nothing
}
```

---
### Next: [Sailfish Variables](../3/sailfish-variables.md)