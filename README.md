# Veer-Performa - an unambitious performace test runner
A .net package used to perform low resolution performance analysis of your component or API.

# Intended Use
This test framework is NOT intended to produce high resolution (microsecond, nanosecond) results on performance. It
IS intended to provide approximate millisecond resolution.

Although you may use this project however you'd like, the intended use case for this is to provide approximate millisecond
response time data for API calls that you're developing against.

For this reason, this project does not go to the extent that other more rigorous benchmark analysis tools such as, say, BenchmarkDotNet do. BenchmarkDotNet in particular does take a clean approach to constructing the test classes, so that idea has been incorporated here when specifying `IterationVariable`s.

Thanks to the BenchmarkDotNet team for that pattern. You folks are really smart. *hat tip*

# Veer Performa performance testing tools

When you create a new test class, you provide attributes to:
 - The class - to tell the runner this is a VeerPerforma performance test
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
Naturally, most developers will wish to executes their performance tests in an IDE. This is why we provide the `VeerPerforma.TestAdapter`, which allows you to activate test classes directly from the IDE. You are likely familiar with Visual Studio, or perhaps Jetbrains Rider's, test running tools (the little play button that appears next to your tests). The `DemoTestRunner` project has the test adapter installed and provides a simple test for you to see how this works.

### DemoConsoleApp
If you're looking to run performance tests as part of your automated build and test pipeline, you can use use the `VeerPerformaExecutor` in a console app that is easily invokable. The `DemoConsoleApp` demonstrates this functionality and also provides a simple command line interface using the `McMaster.Extensions.CommandLineUtils`, which is a neat simple way to provide CLI args.


# Example Test Case

The first question one might ask about a new tool is 'what are all the possible features of this tool'?

The following class demonstrates all of the attributes currently available, as well as all available arguments (with made-up parameters).

```
[VeerPerforma(numIterations = 3, numWarmupIterations = 3)]
public class DemoPerfTest : ApiTestBase
{
    [InDevelopment("This is a note to the reader: this attribute is not ready for use and currently does the same thing as method setup.")]
    [VeerGlobalSetup]
    public void GlobalSetup()
    {
        // This happens once per test case instance, and happens before any other setup methods.
        // You might do something here like create a database.
    }

    [InDevelopment("This is a note to the reader: this attribute is not ready for use and currently does the same thing as method teardown.")]
    [VeerGlobalTeardown]
    public void GlobalTeardown()
    {
        // This happens once per test case instance, and happens after all otehr teardown methods.
    }

    [VeerExecutionMethodSetup]
    public void ExecutionMethodSetup()
    {
        // This happens once per method. It occurs before method iterations.
    }

    [VeerExecutionMethodTeardown]
    public void ExecutionMethodTeardown()
    {
        // This happens once per method. It occurs after method iterations.
    }

    [VeerExecutionIterationSetup]
    public void IterationSetup()
    {
        // This is a high frequency setup method that happens before every invocation of the main execution method (with the `ExecutePerformanceCheck` attribute).
    }

    [VeerExecutionIterationTeardown]
    public void IterationTeardown()
    {
        // This is a high frequency setup method that happens after every invocation of the main execution method (with the `ExecutePerformanceCheck` attribute).
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

