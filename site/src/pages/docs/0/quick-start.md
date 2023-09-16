---
title: Quick Start Guide
---


## 1. Create a Test Project

Create a class library project and install the [Sailfish Test Adapter](https://www.nuget.org/packages/Sailfish.TestAdapter);


## 2. Write a Sailfish Test

```csharp

[Sailfish]
public class Example
{
    private readonly IClient client;

    [SailfishVariable(1, 10)]
    public int N { get; set; }

    public Example(IClient client)
    {
        this.client = client;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await client.Get("/api", ct);
    }
}
```

## 3. Register a Dependency

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.Register(_ => typeInstance).As<IClient>();
    }
}
```

## 4. Inspect your results

```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |   111.1442 |
| Median |   107.8113 |
| StdDev |     7.4208 |
| Min    |   105.9743 |
| Max    |   119.6471 |


Outliers Removed (0)
--------------------

Adjusted Distribution (ms)
--------------------------
119.6471, 105.9743, 107.8113
```
