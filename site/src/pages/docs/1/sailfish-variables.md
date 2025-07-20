---
title: Sailfish Variables
---

Sailfish variables are a powerful feature that allows you to create multiple test cases with different test class states, enabling comprehensive performance analysis across various scenarios.

{% info-callout title="What Are Variables?" %}
Sailfish variables allow you to parameterize your performance tests, running the same test method with different input values to understand how performance scales with different parameters.
{% /info-callout %}

## 🔢 Variable Types

Sailfish provides two types of variable attributes to suit different testing scenarios:

{% feature-grid columns=2 %}
  {% feature-card
    title="SailfishVariable"
    description="Define specific discrete values to test with explicit control over each test case."
  /%}

  {% feature-card
    title="SailfishRangeVariable"
    description="Generate a range of values automatically with start, count, and step parameters."
  /%}
{% /feature-grid %}

## 🎯 SailfishVariable Attribute

Use `SailfishVariable` when you want to test specific, discrete values:

### ⚡ Basic Usage

Apply the attribute to a public property with your test values:

```csharp
[Sailfish]
public class Example
{
    [SailfishVariable(10, 100, 1000)] // params object[]
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod);
    }
}
```

{% tip-callout title="Flexible Values" %}
You can pass any number of values of any type that can be converted to the property type.
{% /tip-callout %}

### 🔄 Multiple Variables

Combine multiple variables for comprehensive testing:

```csharp
[Sailfish]
public class DatabaseTest
{
    [SailfishVariable(10, 100, 1000)]
    public int RecordCount { get; set; }

    [SailfishVariable("SELECT", "INSERT", "UPDATE")]
    public string OperationType { get; set; }

    [SailfishMethod]
    public async Task TestDatabaseOperation()
    {
        // Test will run for each combination:
        // (10, "SELECT"), (10, "INSERT"), (10, "UPDATE")
        // (100, "SELECT"), (100, "INSERT"), (100, "UPDATE")
        // (1000, "SELECT"), (1000, "INSERT"), (1000, "UPDATE")
    }
}
```

## 📊 SailfishRangeVariable Attribute

Use `SailfishRangeVariable` for systematic testing across a range of values:

### 📈 Basic Range

Generate values automatically with start, count, and step:

```csharp
[Sailfish]
public class Example
{
    [SailfishRangeVariable(start: 1, count: 3, step: 100)]
    public int SleepPeriod { get; set; }
    // Generates: 1, 101, 201

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod);
    }
}
```

### 🧠 Complexity Analysis

Enable complexity analysis for algorithmic performance testing:

```csharp
[Sailfish]
public class AlgorithmTest
{
    [SailfishRangeVariable(true, start: 100, count: 5, step: 100)]
    public int DataSize { get; set; }
    // Generates: 100, 200, 300, 400, 500
    // First parameter 'true' enables complexity analysis

    [SailfishMethod]
    public void SortAlgorithm()
    {
        var data = GenerateTestData(DataSize);
        QuickSort(data);
    }
}
```

{% code-callout title="Machine Learning Analysis" %}
When complexity analysis is enabled, Sailfish uses machine learning to determine the algorithmic complexity (Big O) of your code.
{% /code-callout %}

## 💡 Best Practices

{% tip-callout title="Variable Design Guidelines" %}
Follow these best practices to get the most out of Sailfish variables in your performance tests.
{% /tip-callout %}

### 🎯 Choosing Values

{% feature-grid columns=2 %}
  {% feature-card
    title="Representative Values"
    description="Choose values that represent real-world usage patterns and edge cases."
  /%}

  {% feature-card
    title="Scaling Patterns"
    description="Include values that help identify performance scaling characteristics."
  /%}
{% /feature-grid %}

**Good variable choices:**
- **Small, Medium, Large**: Test different scales (e.g., 10, 100, 1000 records)
- **Edge Cases**: Include boundary values (0, 1, maximum expected)
- **Real-World Values**: Use actual production data sizes when possible

### ⚡ Performance Considerations

{% warning-callout title="Test Execution Time" %}
Remember that Sailfish will run your test method for every combination of variables. More variables and values mean longer test execution times.
{% /warning-callout %}

**Optimization strategies:**
- Start with fewer values and add more as needed
- Use `SailfishRangeVariable` for systematic exploration
- Consider the total number of combinations (variables multiply)

## 🚀 Advanced Usage

### 🔧 Custom Variable Types

Variables work with any type that can be converted from the provided values:

```csharp
[Sailfish]
public class CustomTypeTest
{
    [SailfishVariable("2023-01-01", "2023-06-01", "2023-12-31")]
    public DateTime TestDate { get; set; }

    [SailfishVariable(1.5, 2.0, 2.5)]
    public double Multiplier { get; set; }

    [SailfishMethod]
    public void ProcessData()
    {
        // Your test logic here
    }
}
```

### 💉 Dependency Injection with Variables

Variables work seamlessly with dependency injection:

```csharp
[Sailfish]
public class ServiceTest
{
    private readonly IDataService dataService;

    [SailfishVariable(10, 100, 1000)]
    public int BatchSize { get; set; }

    public ServiceTest(IDataService dataService)
    {
        this.dataService = dataService;
    }

    [SailfishMethod]
    public async Task ProcessBatch()
    {
        await dataService.ProcessBatchAsync(BatchSize);
    }
}
```

## 🧠 Complexity Estimation (ScaleFish)

{% code-callout title="Machine Learning Analysis" %}
Enable ScaleFish complexity estimation to automatically determine the algorithmic complexity (Big O) of your code using machine learning.
{% /code-callout %}

When applying a variable attribute, you may choose to specify that variable for ScaleFish complexity estimation and modeling. To do so set the first optional parameter to true:

```csharp
[SailfishVariable(scalefish: true, 10, 100, 1000)]
```

{% warning-callout title="Integer Variables Required" %}
**NOTE**: When using Scalefish, variables must be of type `int` for proper complexity analysis.
{% /warning-callout %}

{% info-callout title="Next Steps" %}
Now that you understand variables, learn about the [Test Lifecycle](/docs/1/sailfish-test-lifecycle) to understand how Sailfish executes your tests, or explore [Test Dependencies](/docs/1/test-dependencies) for advanced dependency injection scenarios.
{% /info-callout %}