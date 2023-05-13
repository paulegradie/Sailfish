---
title: Quick Start Guide
---

Follow these steps to stand up a simple test project. This guide assumes you've already created a solution and are ready to add a new project.

## 1. Create a project

Create a class library project and delete any default files. Install the [Sailfish Test Adapter](https://www.nuget.org/packages/Sailfish.TestAdapter);

    dotnet add package Sailfish.TestAdapter

## 2. Write a Sailfish Test

```csharp
public class ReadmeExample
{
    private readonly IClient myClient;

    [SailfishVariable(1, 10)]
    public int N { get; set; }

    public ReadmeExample(IClient myClient) // type is injected so long as its registered
    {
        this.myClient = myClient;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        var tasks = Enumerable.Range(0, N).Select(_ => mylient.Get("/api", cancellationToken));
        await Task.WhenAll(tasks);
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
       builder.RegisterType(typeInstance).As<IClient>();
    }
}
```

## 4. Inspect your results

```
-----------------------------------
ReadmeExample
-----------------------------------

 | DisplayName                      | Median  | Mean         | StdDev     | Variance |
 |-----------------------------------------------------------------------------------|
 | ReadmeExample.TestMethod(N: 1)   | 109 ms  | 108.66667 ms | 3.01109 ms | 9.06667  |
 | ReadmeExample.TestMethod(N: 10)  | 1010 ms | 1011.5 ms    | 8.96103 ms | 80.3     |

-----------------------------------
WilcoxonRankSumTest results comparing:
Before: ~\tracking_directory\PerformanceTracking_2023-19-2--09-02-00.csv.tracking
After: ~\tracking_directory\PerformanceTracking_2023-19-2--09-02-51.csv.tracking
-----------------------------------
Note: The change in execution time is significant if the PValue is less than 0.005

 | DisplayName | MeanOfBefore | MeanOfAfter  | MedianOfBefore | MedianOfAfter | PValue | TestStatistic | ChangeDescription |
 |-------------------------------------------------------------------------------------------------------------------------|
 | ReadmeExample.TestMethod(N: 1)   | 108.66 ms  | 111.16 ms  | 109 ms   | 111 ms     | 0.3008  | 25   | No Change   |
 | ReadmeExample.TestMethod(N: 10)  | 1011.5 ms  | 1013.33 ms | 1010 ms  | 1013.5 ms  | 0.5541  | 22   | No Change   |


No regressions or improvements found.
Test run was valid
```
