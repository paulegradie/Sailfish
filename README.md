<h1 align="center" style="flex-direction: column;"><img src="assets/Sailfish.png" alt="Sailfish" width="700" /></h1>

Sailfish is a .NET library used to perform low resolution performance analysis of your component or API.

Available on [https://www.nuget.org/packages/Sailfish/](https://www.nuget.org/packages/Sailfish/)

## [Visit the Wiki](https://github.com/paulegradie/Sailfish/wiki)

Visit the [wiki](https://github.com/paulegradie/Sailfish/wiki) to view the [getting started guide](https://github.com/paulegradie/Sailfish/wiki/Using-Sailfish-as-a-C%23-console-app) and other helpful details.

## Quick Start Guide

```csharp
class Program : SailfishProgramBase
{
    public static async Task Main(string[] testNamesToFilterBy)
    {
        // provided via the SailfishProgramBase
        await SailfishMain<Program>(testNamesToFilterBy);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
       // provide types from the same assembly as your performance tests
       return new[] { GetType() };
    }

    protected override void RegisterWithSailfish(ContainerBuilder builder)
    {
       // register anything that you need to inject into your performance tests
       builder.RegisterType<MyType>().AsSelf();
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

## General Performance testing

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
