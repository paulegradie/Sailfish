---
title: Sailfish Variables
---

A powerful feature of the Sailfish library is the ability to define variables that can be used in your tests.

Sailfish variables are properties on your class that are decorated with a `[SailfishVariable]` attribute. The attribute takes a `params` array of any constant object type, which will be used to form test cases for your test method.

> **warning** Sailfish does not yet attempt to protect against the inclusion of mixed runtime types in the SailfishVariableAttribute constructor. In the future, an analyzer will be provided to to provide compilation errors in this case.

## An Example

Here is an example of a test that defines a single test variable:

```csharp
[Sailfish]
public class TestWithSingleVariable
{

    [SailfishVariable(10, 100, 1000)] // the sailfish variable attribute with 3 values. This will result in 3 test cases for each test method
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {

        await Task.Sleep(SleepPeriod, ct)
    }
}
```

## An example with string variables

```csharp
[Sailfish(NumIterations = 3)]
public class TestWithMuchVariable
{
    private readonly IConfiguration configuration = null!;
    private readonly IClient client = null!;
    private readonly Dictionary<string, ScenarioData> = new();

    public TestWithMuchVariable(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string Scenario { get; set; }

    [SailfishGlobalSetup]
    public void Setup()
    {
        scenarioMap.Add(ScenarioA, new ScenarioData(configuration.Get<Scenario>(ScenarioA));
        scenarioMap.Add(ScenarioB, new ScenarioData(configuration.Get<Scenario>(ScenarioB));
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        client = ClientFactory.CreateClient(scenarioMap[Scenario].Url)
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await client.createA(NumAToCreate, ct);
    }
}
```

## Test Cases Based on the Variable Tensor

> **Note**
> A `tensor` is a generic algebraic term for an object that holds nubmers.
>
> The following are all tensors:
>
> ```markdown
> - a scalar (rank-0)
>   1
>
> - a vector (rank-1)
>   [1, 2, 3]
>
> - a matrix (rank-2)
>   [[1, 2, 3],
> [4, 5, 6],
> [7, 8, 9]]
> ```
>
> You can image that a rank-3 tensor would take on something of a cube shape. However, we don't have a specific word for rank-(N + 3) tensors. We just call them `tensors`.
>
> This particular word is appropriate for describing the variable combinations that are produced when using `SailfishVariables`.

When we use sailfish variables, Sailfish will create combinations of these variables and apply them to test cases.

If we do not define any variables, we will simply execute the test method `NumIterations` times.

If we define one variable with `N` values, we'll create `N` test cases for each `SailfishMethod`. If we define two variables with `N` and `M` values, we'll create `N x M` test cases for each `SailfishMethod`. And so on and so forth.

> **Warning**: Be careful with how you design your test classes. Sailfish will execute test cases for every variable combination - even if the method doesn't make use of any of the variables.

## Loaded Example

```csharp
[Sailfish(NumIterations = 3)]
public class TestWithMuchVariable
{
    private readonly IConfiguration configuration = null!;
    private readonly IClient client = null!;

    public TestWithMuchVariable(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = ScenarioB;

    [SailfishVariable(10, 100, 1000)]
    public int SleepPeriod { get; set; }

    [SailfishVariable(5, 10, 100)]
    public int NumAToCreate { get; set; }

    [SailfishVariable(5, 10, 100)]
    public int NumBToCreate { get; set; }

    [SailfishVariable("scenarioA", ScenarioB)]
    public string Scenario { get; set; }

    [SailfishGlobalSetup]
    public void Setup()
    {
        scenarioMap = new Dictionary<string, ScenarioData>()
        {
            {"scenarioA", new ScenarioData(configuration.Get<Scenario>(ScenarioA))},
            {ScenarioB, new ScenarioData(configuration.Get<Scenario>(ScenarioB))}
        }
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        client = ClientFactory.CreateClient(scenarioMap[Scenario].Url)
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await Task.Sleep(SleepPeriod, ct);
        await client.createA(NumAToCreate, ct);
        await client.createB(NumBToCreate, ct);
    }

    [SailfishMethod]
    public async Task TestMethodB(CancellationToken ct)
    {
        await client.createA(NumAToCreate, ct);
        await Task.Sleep(SleepPeriod, ct); // shifted this down between the other methods
        await client.createB(NumBToCreate, ct);
    }

    [SailfishIterationTeardown]
    public async Task IterationTeardown(CancellationToken ct)
    {
        await client.destroyAllA();
        await client.destroyAllB();
    }
}
```

This example will result in the creation of 54 test cases (3 x 3 x 3 \* 2).

> **Warning**: Avoid the mistake of designing tests that use multiple sailfish methods and define multiple SailfishVariables, but only some tests utilize certain variables. If you find yourself in this situation, split your methods into separate test classes.
