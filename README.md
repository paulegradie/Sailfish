<div style="display: flex; justify-content: center"><img src="assets/Sailfish.png" alt="Sailfish" width="700" /></div>


<h3 align="center">

Available on [Nuget](https://www.nuget.org/packages/Sailfish/)
</h3>

<h3 align="center" style="display: flex; flex-direction: row; justify-content: center;">

![GitHub Workflow Status (with branch)](https://img.shields.io/github/actions/workflow/status/paulegradie/sailfish/publish.yml)
![Nuget](https://img.shields.io/nuget/dt/Sailfish)
</h3>

<h3 align="center">
✨Sailfish tests are now able to be run directly from the IDE!✨
</h3>


# Introduction
**Sailfish is a .NET library that you can use to write performance tests that are**:
 - styled in a simple, consistent, familiar way
 - run in process (for easy debugging and development)
 - millisecond-resolution 
 - sychronous or asynchronous
 - flexible and controllable via lifecycle methods
 - executable via:
    - a console app
    - the ✨ IDE ✨ (using the new `Sailfish.TestAdapter`)
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



# Getting Started
For guides on how to effectively use Sailfish, please consider [visiting the Wiki](https://github.com/paulegradie/Sailfish/wiki). There you will find:
 - the [full getting started guide](https://github.com/paulegradie/Sailfish/wiki/Using-Sailfish-as-a-C%23-console-app) 
 - in depth information on how Sailfish works
 - details on how to make the most of its feature set

## Quick Start Guide

There are several options to choose from when setting up your Sailfish test project. You can:
 1. create a console app that:
    - uses the built-in command line tool (recommended for simple quick-start apps)
    - uses your own execution logic (recommended for more production oriented applications)
 2. create a class library project and install the `Sailfish.TestAdapter` and run your tests from the IDE
 3. create a class library project for your tests and a separate console app project (that references the class library) and **get the best of both worlds**. 


### Option 1: A basic console app using the builtin program base

This example provides an example implementation for:
 - a quick-start console app

```csharp
class Program : SailfishProgramBase
{
    public static async Task Main(string[] testNamesToFilterBy)
    {
        await SailfishMain<Program>(testNamesToFilterBy);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
       return new[] { GetType() };
    }

    protected override RegistrationTypeProvider()
    {
        return new[] {typeof(RegistrationProvider)}
    }
}

// this will be picked up by Sailfish's dependency scanner
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       builder.RegisterType<MyType>().AsSelf();
       await Task.Yield();
    }

}
[Sailfish]
public class AMostBasicTest
{
    private readonly MyType myType;

    public AMostBasicTest(MyType myType)
    {
        this.myType = myType;
    }

    [SailfishMethod] // checkout all of the attributes provided by sailfish
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        await Task.Delay(2000, cancellationToken);
    }
}
```

### A most basic test

```csharp
```

## Critical Details

**Tests are always run in sequence**

Sailfish does not parallelize test executions. The simple reason is that we are assessing how quickly your code executes and by parallelizing tests, the execution time would likely increase. To eliminate noise test neighbors on the machine executing the tests, only one test runs at a time.

**Tests are run in ordered sequence**

Sailfish does not currently randomize test order execution.


## Limitations

Benchmarking software or hardware often involves taking precise measurements on stable, controlled hardware using highly optimized tools and protocols. Furthermore, understanding software efficiency often involves using algorithm complexity analysis.

For productionized systems, this tends to be a somewhat academic pursuit. Instead, production systems tend to require performance trends data, as apposed to snapshot data. For example, as you ship new versions of your API, its a good idea to keep an eye on response times.

Sailfish provides the tool that can be used to write performance type interrogative tests that run against your application, and handle the creating of tracking data that you can then provide to consumers (e.g. dashboard apps, notification tools, etc).


## Using Sailfish to write and execute performance tests

Sailfish is a system that collects and executes your Sailfish tests. When you write a test, you simply create a class and mark it with attributes provided by the Sailfish library. At runtime, Sailfish will discover your tests (by looking for types annotated with the SailfishAttribute using reflection), instantiate them, and then call the implemented execution methods in a specific way - all the while recording how long everything took.

So to use sailfish, you'll simply write your tests, and then invoke sailfish's main execution method (e.g. `SailfishRunner.Run`)


## Intended Use

Sailfish is intended to be used as a quick-to-setup console app for local testing, or as part of a productionized performance monitoring system. Execution of Sailfish tests is kept in process (to improve your development experience) at the expense of certain internal optimizations. For this reason, results produced by Sailfish have a minimum resolution of milliseconds.

Sailfish includes an optional (opt-in) outlier removal, which can truncate the outer quartiles of your performance result data. So if you're running Sailfish tests against an application running in the cloud (for example as part of a live system performance monitoring application) you can take the approach of increasing the number of samples you collect (over different parts of the day) and removing the first and fourth quartiles (to remove the majority of outlier data).

Sailfish does not go to the extent that other more rigorous benchmark analysis tools such as, say, BenchmarkDotNet do to mitigate the effects of hardware, compute sharing, or general compilation concerns. Sailfish will, however, perform warmup executions.

## A Production Performance Monitor

You can use Sailfish to create a continuous monitor that will execute your performance tests against your product in a production setting. To do this, imagine an application that runs a loop - and every time the loop executes, you register a client with the Sailfish internal registrations (so that it can be used in your test). Then, you register a custom `WriteCurrentTrackingFileCommandHandler` implementation that saves your tracking data to a persistence location of your choosing. With this data in place, your application can execute t-tests to look for significant differences between runs. In this way, you can produce performance monitor data for versions of your software as its deployed, and consume this for presenting or notifications (if you, say, discover a regression).

Please visit our wiki for examples on how to use Sailfish effectively for your project or organization.


## License
Sailfish is [MIT licensed](./LICENSE).

## Acknowledgements

Sailfish is inspired by the [BenchmarkDotNet](https://benchmarkdotnet.org/) precision benchmarking library.
