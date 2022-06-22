<h1 align="center" style="flex-direction: column;"><img src="assets/Sailfish.png" alt="Sailfish" width="700" /></h1>

Sailfish is a .net library used to perform low resolution performance analysis of your component or API.

# Intended Use
This test framework is intended to provide approximate millisecond resolution performance data of your component or API. It is NOT intended to produce high resolution (microsecond, nanosecond) results on performance.

You may use this project however you'd like, however the intended use case for this is to provide approximate millisecond
response time data for API calls that you're developing against. Please keep in mind the physical machines that you run this software will have a direct affect on the results that are produced. In otherwords, for more reliable results, execute tests on stable hardware that is, if possible, not really doing anything else. For example, running these tests on dynamic cloud infrastructure may introduce signficant outlier results.

Fortunately, tools to mitigate the affects of such volatility in the infrastructure are currently under development.

For this reason, this project does not go to the extent that other more rigorous benchmark analysis tools such as, say, BenchmarkDotNet do.

# Example Test Case

The following class demonstrates all of the attributes currently available, as well as all available arguments (with made-up parameters). We'll provide a description of this class in the next section.

```
[Sailfish(numIterations = 3, numWarmupIterations = 3)]
public class DemoPerfTest : ApiTestBase
{
    [InDevelopment("This is a note to the reader: this attribute is not ready for use and currently does the same thing as method setup.")]
    [SailGlobalSetup]
    public void GlobalSetup()
    {
        // This happens once per test case instance, and happens before any other setup methods.
        // You might do something here like create a database.
    }

    [InDevelopment("This is a note to the reader: this attribute is not ready for use and currently does the same thing as method teardown.")]
    [SailGlobalTeardown]
    public void GlobalTeardown()
    {
        // This happens once per test case instance, and happens after all otehr teardown methods.
    }

    [SailExecutionMethodSetup]
    public void ExecutionMethodSetup()
    {
        // This happens once per method. It occurs before method iterations.
    }

    [SailExecutionMethodTeardown]
    public void ExecutionMethodTeardown()
    {
        // This happens once per method. It occurs after method iterations.
    }

    [SailExecutionIterationSetup]
    public void IterationSetup()
    {
        // This is a high frequency setup method that happens before every invocation of the main execution method (with the `ExecutePerformanceCheck` attribute)
    }

    [SailExecutionIterationTeardown]
    public void IterationTeardown()
    {
        // This is a high frequency setup method that happens after every invocation of the main execution method (with the `ExecutePerformanceCheck` attribute)
    }

    [IterationVariable(1, 2, 3)]
    public int NTries { get; set; }

    [IterationVariable(200, 300)]
    public int WaitPeriod { get; set; }


    [ExecutePerformanceCheck]
    public async Task WaitPeriodPerfTest()
    {
        var test = new TestCollector();
        Thread.Sleep(WaitPeriod);
        await Client.GetStringAsync("/");
        WriteSomething();
    }

    private void WriteSomething()
    {
        Console.WriteLine($"Wait Period - Iteration Complete: {NTries}-{WaitPeriod}");
    }

    public DemoPerfTest(WebApplicationFactory<MyApp> factory) : base(factory)
    {
    }
}
```

# How does it work?

When you create a new test class, you provide attributes to:
 - The class - to tell the runner this is a Sailfish performance test
 - Helper Methods - to tell the runner to call certain methods before and after the main execution iterations
 - The main execution method - a single method that will be called by the runner and clocked for speed.

```
[Sailfish(numIterations = 3, numWarmupIterations = 3)]
public class DemoPerfTest : ApiTestBase
```

When the test executor is invoked (either as a console app, or via the IDE test play button tools), test classses are discovered via reflection. A quick validation check will be made (in the event that you have incorrectly created a test class or have provided test names that don't exist in your library), and then your tests will be constructed.

Any properties that have an `IterationVariable` attribute will compiled into a parameter grid. This is important because the more iteration variables you supply, and the more parameters you supply, the more test case instances will be activated from the class. This is a product operation, where you multiple the lengths of all the iteration variable params arrays together.

The class above provides two iterations variables:

    [IterationVariable(1, 2, 3)]        // 3 variables here
    public int NTries { get; set; }

    [IterationVariable(200, 300)]      // and 2 variables here
    public int WaitPeriod { get; set; }

Therefore - the total number of test cases produced will be 6 (because `3 * 2`).

Finally, once all class instances are built, the test runner iterates through them, invokes the setup methods, invokes the execution methods (according to the number of iterations specified in the class attribute), times those invocations and logs them, and then executes teardown methods.

# Usage Scenarios

There are two demo projects provided in this repo that demonstrate two typical use cases.

### DemoTestRunner
Naturally, most developers will wish to executes their performance tests in an IDE. This is why we provide the `Sailfish.TestAdapter`, which allows you to activate test classes directly from the IDE. You are likely familiar with Visual Studio, or perhaps Jetbrains Rider's, test running tools (the little play button that appears next to your tests). The `DemoTestRunner` project has the test adapter installed and provides a simple test for you to see how this works.

### DemoConsoleApp
If you're looking to run performance tests as part of your automated build and test pipeline, you can use use the `SailfishExecutor` in a console app that is easily invokable. The `DemoConsoleApp` demonstrates this functionality and also provides a simple command line interface using the `McMaster.Extensions.CommandLineUtils`, which is a neat simple way to provide CLI args.


# Features

## Performance tracking

Sailfish by default will print performance results to console (unless the `[SupressConsole]` attribute is applied to the test class). Additional attributes are provided to write csv and markdown files to a nominated directory.

 - `[WriteToMarkdown]`
 - `[WriteToCsv]`

## Differential Analyzer

Sailfish provides basic statistical testing tools to determine differences between your code versions. These are largely automatic, and on by default. You may however configure them to a lesser extent.


When a directory is nominated, Sailfish will keep a (chonologically ordered) history of tracking files. Analysis will be executed on the latest file in the history, or you may choose a specific file to compare against.

For example, say you were working on refactoring a feature in your API to improve its efficiency. To analyze the difference, write a performance test on your default branch (e.g. main) of your version control system (e.g. git). Next, go to your branch code and execute the performance test app again. Sailfish will find the default branch performance results and then use them in a two tailed students t test. (In the future, we hope to use the Welches t-test).

There are two modes for tracking and analysis.
 - Tracking - this mode will write files to the tracking directory
 - Analysis - this mode will compare the current execution to the previous

When the program executes in tracking mode, a tracking file will be emitted. Then, the statistical analyzer will read the latest, and the one that came just before.

**The alpha value for change description is 0.01.**

**Future Directions**
 - The console app should expose a database schema and return a json of data perhaps.
 - The test adapter should also be able to retrieve the last set of results.


# Example output

For an example of execution outputs, have a look at the [Console App demo project readme](./source/AsAConsoleApp/README.md), where performance results and statistical analyses are presented.

# RoadMap

While Sailfish is ready for basic usage given the features outlined above, there are few outstanding things to complete before Sailfish is v1 complete.

 - Hardware affect mitigation

We can introduce options for users to remove outliers from their datasets, for example by computer quartiles and truncating the resulting datasets.

 - Complexity estimation

An interesting use case for load or performance testing is emperically determining algorithm complexity. To accomplish this, we could provide a test class attribute that indicates a particular test case for such a test. To facilitate the analysis, the user would design their test case in such a way that a single IterationVariable attribute delivered a series representing linear growth in load. Once the execution method results are collected, we would perform various regressions against the data to attempt a 'best guess' on the type of curve represented by the data. This could then be printed in addition to the performance results for the method as an addendum to the results table for that method.

 - Git integration

A common use case we've seen is the before and after analysis of changes made between two branches. When using Sailfish as a console app, we could allow users to provide two branch names (before and after) to then automate a comparative analysis of their branches. The tool would set the working directory to be the project directory, attempt to switch to the before 'branch', build, execute, switch branches to the 'after' branch, and repeate. The same statistical analysis would be emitted after.

- Custom event handlers

We could provide a mediator that allow users to register message handlers that perform custom actions. This could be accomplished by using MediatR internally and allowing users to register handler implementations - which we would that call as part of the execution run.

 - Publishing nuget packages!

Sailfish is intended for public consumption, so we need to get a github action set up to publish the library to nuget.


# License
Sailfish is [MIT licensed](./LICENSE).

# Acknowledgements

Sailfish is inspired by the [BenchmarkDotNet](https://benchmarkdotnet.org/) precision benchmarking library.