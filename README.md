<h3 align="center"><div style="display: flex; justify-content: center"><img src="assets/Sailfish.png" alt="Sailfish" /></div></h3>

<h3 align="center">

Available on [Nuget](https://www.nuget.org/packages/Sailfish/)

</h3>

<h3 align="center" style="display: flex; flex-direction: row; justify-content: center;">

![GitHub Workflow Status (with branch)](https://img.shields.io/github/actions/workflow/status/paulegradie/sailfish/publish.yml)
![Nuget](https://img.shields.io/nuget/dt/Sailfish)

</h3>

<span align="center">

> **Note**: Sailfish tests are now able to be run directly from the IDE

</span>

<h2 align="center">

[Documentation](https://github.com/paulegradie/Sailfish/blob/main/docs/home.md)

</h2>

# Introduction

**Sailfish is a .NET library that you can use to write performance tests that are**:

- styled in a simple, consistent, familiar way
- run in process (for easy debugging and development)
- millisecond-resolution
- sychronous or asynchronous
- flexible and controllable via lifecycle methods
- executable via:
  - a console app
  - your IDE (using the new `Sailfish.TestAdapter`)
- compatible with standard dependency injection

**Sailfish ships with various tools that facilitate**:

- data format conversion between:
  - markdown tables
  - csv tables
  - C# objects
- result tracking:
  - automatically by default
  - extensibilty points that are highly customizable
- data parsing:
  - file I/O (for reading back tracking data)
  - test key parsing (for extracting test case details)
- statistical analysis:
  - comparing normal or non-normal data distributions for pre/post comparative analysis
  - descriptive statistics

## Quick Start Guide

Follow these steps to stand up a simple test project. This guide assumes you've already created a solution and are ready to add a new project.

## 1. Create a project

Create a class library project and delete any default files. Install the [Sailfish Test Adapter](https://www.nuget.org/packages/Sailfish.TestAdapter);

    dotnet add package Sailfish.TestAdapter

## 2. Write a Sailfish Test

```csharp
public class AMostBasicTest
{
    private readonly IClient myClient;

    [SailfishVariable(1, 10)]
    public int N { get; set; }

    public AMostBasicTest(IClient myClient) // type is injected so long as its registered
    {
        this.myClient = myClient;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        await Task.Delay(100 * N, cancellationToken);
        await myClient.GetAll("/api/models", cancellationToken);
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

# Critical Details

## **Tests are always run in sequence**

Sailfish does not parallelize test executions. The simple reason is that we are assessing how quickly your code executes and by parallelizing tests, the execution time would likely increase. To eliminate noise test neighbors on the machine executing the tests, only one test runs at a time.

## **Tests run order is deterministic**

Sailfish does not currently randomize test order execution.

## **Tests are run in-process**

Sailfish does not perform the optimizations necessary to achieve reliable sub-millisecond-resolution results. If you are interested in rigorous benchmarking, please consider using an alternative tool, such as BenchmarkDotNet. Sailfish was produced to remove much of the complexities and boilerplate required to write performance tests that don't need highly optimized execution.

The allows you to debug your tests directly in the IDE without the need to attach to an external process.

## **Test classes are instantiated just-in-time**

Sailfish uses enumerators to ensure that all of your test classes are not instantiated all at the same time. This is very convenient in cases where you are doing a lot of setup work in your constructors - for example if you are creating in memory server instances you wish you run tests against.

# License

Sailfish is [MIT licensed](./LICENSE).

# Acknowledgements

Sailfish is inspired by the [BenchmarkDotNet](https://benchmarkdotnet.org/) precision benchmarking library.
