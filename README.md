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


## Basic Example Console App

**Note**: If you place your tests in the same project as your console app, you will not be able to run them from your IDE. Instead, place them in a separate project, reference that project, and then provide the registration provider to the console app to either the override method show below (if you are using the provided base class) or the `RunSettings` object (if you are invoking the SailfishRunner directly).

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
       var typeInstance = await MyClientFactory.Create(ct);
       builder.RegisterType(typeInstance).As<IClient>();
    }
}

[Sailfish] // default method iteration count is 3
public class AMostBasicTest
{
    private readonly IClient myClient;

    public AMostBasicTest(IClient myClient) // type is injected so long as its registered
    {
        this.myClient = myClient;
    }

    [SailfishMethod] // checkout all of the attributes provided by sailfish
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        await myClient.Get("/api", cancellationToken);
    }
}
```

# Critical Details

## **Tests are always run in sequence**

Sailfish does not parallelize test executions. The simple reason is that we are assessing how quickly your code executes and by parallelizing tests, the execution time would likely increase. To eliminate noise test neighbors on the machine executing the tests, only one test runs at a time.

## **Tests run order is deterministic**

Sailfish does not currently randomize test order execution.

## **Tests are run in-process**

Sailfish does not perform the optimizations necessary to achieve reliable sub-millisecond-resolution results. If you are interested in rigorous benchmarking, please consider using an alternative tool, such as BenchmarkDotNet. Sailfish was produced to remove much of the complexities and boilerplate required to write performance tests that don't need highly optimized execution.

The allows you to debug your tests directly in the IDE without the need to attach to an external process.

# License
Sailfish is [MIT licensed](./LICENSE).

# Acknowledgements

Sailfish is inspired by the [BenchmarkDotNet](https://benchmarkdotnet.org/) precision benchmarking library.
