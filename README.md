# Sailfish - an unambitious performace test runner
A .net package used to perform low resolution performance analysis of your component or API.

# Intended Use
This test framework is NOT intended to produce high resolution (microsecond, nanosecond) results on performance. It
IS intended to provide approximate millisecond resolution.

Although you may use this project however you'd like, the intended use case for this is to provide approximate millisecond
response time data for API calls that you're developing against.

For this reason, this project does not go to the extent that other more rigorous benchmark analysis tools such as, say, BenchmarkDotNet do. BenchmarkDotNet in particular does take a clean approach to constructing the test classes, so that idea has been incorporated here when specifying `IterationVariable`s.

Thanks to the BenchmarkDotNet team for that pattern. You folks are really smart. *hat tip*

# Sailfish performance testing tools

When you create a new test class, you provide attributes to:
 - The class - to tell the runner this is a Sailfish performance test
 - Helper Methods - to tell the runner to call certain methods before and after the main execution iterations
 - The main execution method - a single method that will be called by the runner and clocked for speed.

# How does it work?

When the test executor is invoked (either as a console app, or via the TestAdapter), your performance test class (e.g. the one above) is manipulated using reflection. A quick validation check will be made (in the event that you have incorrectly created a test class or have provded test names that don't exist in your library), and then your tests will be constructed and executed.

Any properties that have an `IterationVariable` attribute will compiled into a parameter grid. This is important because the more iteration variables you supply, and the more parameters you supply, the more test case instances will be activated from the class. This is product operation, where you multiple the lengths of all the iteration variable params arrays together.

The class above provides two iterations variables:

    [IterationVariable(1, 2, 3)]        // 3 variables here
    public int NTries { get; set; }

    [IterationVariable(200, 300)]      // and 2 variables here
    public int WaitPeriod { get; set; }

Therefore - the total number of test cases produced will be 6 (because `3 * 2*`).

Finally, once all class instances are built, the test runner iterates through them, invokes the setup methods, invokes the execution methods (according to the number of iterations specified in the class attribute), times those invocations and logs them, and then executes teardown methods.

# Usage Scenarios

There are two demo projects provided in this repo that demonstrate two typical use cases.


### DemoTestRunner
Naturally, most developers will wish to executes their performance tests in an IDE. This is why we provide the `Sailfish.TestAdapter`, which allows you to activate test classes directly from the IDE. You are likely familiar with Visual Studio, or perhaps Jetbrains Rider's, test running tools (the little play button that appears next to your tests). The `DemoTestRunner` project has the test adapter installed and provides a simple test for you to see how this works.

### DemoConsoleApp
If you're looking to run performance tests as part of your automated build and test pipeline, you can use use the `SailfishExecutor` in a console app that is easily invokable. The `DemoConsoleApp` demonstrates this functionality and also provides a simple command line interface using the `McMaster.Extensions.CommandLineUtils`, which is a neat simple way to provide CLI args.


# Example Test Case

The first question one might ask about a new tool is 'what are all the possible features of this tool'?

The following class demonstrates all of the attributes currently available, as well as all available arguments (with made-up parameters).

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

# Features

### Performance tracking



### Differential Analyzer

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

## Ideas
 - git integration - where you can perform an auto branch swap, and the branch names are used in the file names, and the comparison is made automatically.
 - Slack integraiton - when a change in performance is observed - a mediator should be provided to emit a message to a lot of different handlers, which talk to various different services.