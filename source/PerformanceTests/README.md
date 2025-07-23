# 🚀 Sailfish Performance Testing Examples

[![Sailfish](../../assets/Sailfish.png)](https://github.com/paulegradie/Sailfish)

Welcome to the **Sailfish Performance Testing Examples** project! This comprehensive collection demonstrates the full power and flexibility of the Sailfish performance testing framework through real-world examples and best practices.

## 📋 Table of Contents

- [🎯 Overview](#-overview)
- [🏃‍♂️ Quick Start](#️-quick-start)
- [📚 Example Test Categories](#-example-test-categories)
- [🔧 Running Tests](#-running-tests)
- [📊 Understanding Results](#-understanding-results)
- [🎨 Best Practices](#-best-practices)
- [🔗 Additional Resources](#-additional-resources)

## 🎯 Overview

This project showcases **Sailfish**, a powerful .NET performance testing framework that enables:

- **Precise Performance Measurement** with statistical analysis
- **Parameterized Testing** with multiple variable combinations
- **Scenario-Based Testing** for complex real-world scenarios
- **IDE Integration** with test adapter support
- **Rich Output Formats** including Markdown, CSV, and console reports
- **Dependency Injection** support for complex test setups

## 🏃‍♂️ Quick Start

### Prerequisites
- .NET 8.0 or later
- Visual Studio 2022 or JetBrains Rider
- Sailfish NuGet package

### Running Your First Test

1. **Open the solution** in your preferred IDE
2. **Navigate** to any test class in the `ExamplePerformanceTests` folder
3. **Click the play button** next to the class name (not individual methods)
4. **Watch** as Sailfish executes your performance tests and generates detailed reports

> 💡 **Note**: Sailfish uses a single execution method per class pattern. The test runner will automatically discover variables and create parameter combinations.

## 📚 Example Test Categories

### 🌟 **Getting Started Examples**

#### [`ReadmeExample.cs`](ExamplePerformanceTests/ReadmeExample.cs)
**Perfect starting point** - demonstrates basic Sailfish usage with simple variables and async operations.

```csharp
[WriteToMarkdown]
[Sailfish(SampleSize = 3, Disabled = false)]
public class ReadmeExample
{
    [SailfishVariable(1, 10)]
    public int N { get; set; }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken)
    {
        var next = random.Next(50, 450);
        await Task.Delay(next * N, cancellationToken);
    }
}
```

#### [`MinimalTestExample.cs`](ExamplePerformanceTests/MinimalTestExample.cs)
**Simplest possible test** - shows the absolute minimum required for a Sailfish test.

### 🎛️ **Variable System Examples**

#### [`ComplexVariablesExample.cs`](ExamplePerformanceTests/ComplexVariablesExample.cs)
**Advanced variable patterns** - demonstrates the new `ISailfishVariables<T, TProvider>` pattern for complex objects.

#### [`SailfishVariablesClassExample.cs`](ExamplePerformanceTests/SailfishVariablesClassExample.cs)
**Type-safe variables** - shows the `SailfishVariables<T, TProvider>` class pattern for better type safety.

#### [`BackwardCompatibilityExample.cs`](ExamplePerformanceTests/BackwardCompatibilityExample.cs)
**Migration guide** - demonstrates how new features work alongside existing patterns.

### 🎭 **Scenario-Based Testing**

#### [`ScenariosExample.cs`](ExamplePerformanceTests/ScenariosExample.cs)
**Real-world scenarios** - compares performance across different connection types (FTP, HTTPS, Database).

```csharp
[SailfishVariable(ScenarioA, ScenarioB, ScenarioC)]
public string Scenario { get; set; } = null!;

// Tests FTP vs HTTPS vs Database performance characteristics
```

### 🔧 **Advanced Features**

#### [`AllTheFeatures.cs`](ExamplePerformanceTests/AllTheFeatures.cs)
**Kitchen sink example** - showcases dependency injection, multiple setup/teardown methods, and advanced configuration.

#### [`OrderingExample.cs`](ExamplePerformanceTests/OrderingExample.cs)
**Method execution order** - demonstrates how to control test method execution sequence.

#### [`ExceptionExample.cs`](ExamplePerformanceTests/ExceptionExample.cs)
**Error handling** - shows how Sailfish handles exceptions and different return types.

### 📈 **Algorithmic Complexity Analysis**

#### [`ScaleFishExample.cs`](ExamplePerformanceTests/ScaleFishExample.cs)
**Performance scaling** - analyzes how algorithms perform with different input sizes (Linear, Quadratic, N Log N).

### 🌐 **Real-World Integration**

#### [`OctopusClientTest.cs`](ExamplePerformanceTests/OctopusClientTest.cs)
**External API testing** - demonstrates performance testing with real HTTP clients and external dependencies.

## 🔧 Running Tests

### IDE Integration

**Visual Studio 2022 / JetBrains Rider:**
- Navigate to any test class
- Click the ▶️ play button next to the class name
- View results in the test output window

### Console Application

Run the included console application for batch execution:

```bash
dotnet run --project ConsoleAppDemo
```

### Programmatic Execution

```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .TestsFromAssembliesContaining(typeof(ReadmeExample))
    .WithTestNames(typeof(ReadmeExample).FullName!)
    .WithSailDiff()
    .WithScaleFish()
    .Build();

await SailfishRunner.Run(settings);
```

## 📊 Understanding Results

Sailfish generates comprehensive performance reports:

### Console Output
```
ReadmeExample.TestMethod

Descriptive Statistics
----------------------
| Stat   |  Time (ms) |
| ---    | ---        |
| Mean   |   111.1442 |
| Median |   107.8113 |
| StdDev |     7.4208 |
| Min    |   105.9743 |
| Max    |   119.6471 |
```

### Markdown Reports
- Detailed statistical analysis
- Performance comparisons
- Outlier detection
- Visual charts and graphs

### CSV Export
- Raw data for further analysis
- Integration with Excel/BI tools
- Custom reporting pipelines

## 🎨 Best Practices

### ✅ **Do's**

- **Use meaningful variable names** that reflect real-world scenarios
- **Include setup/teardown methods** for proper resource management
- **Add documentation** to explain what each test measures
- **Use appropriate sample sizes** (3-10 for development, 20+ for CI/CD)
- **Combine multiple output formats** for comprehensive reporting

### ❌ **Don'ts**

- **Don't test trivial operations** (simple arithmetic, property access)
- **Avoid external dependencies** in unit performance tests
- **Don't ignore warmup iterations** for JIT-compiled code
- **Avoid overly complex test logic** that obscures what's being measured

### 🏗️ **Test Structure Patterns**

```csharp
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 5, NumWarmupIterations = 2)]
public class MyPerformanceTest
{
    // Variables define test parameters
    [SailfishVariable(10, 100, 1000)]
    public int DataSize { get; set; }

    // Setup runs once per test class
    [SailfishGlobalSetup]
    public void GlobalSetup() { /* Initialize resources */ }

    // Method setup runs before each test method
    [SailfishMethodSetup]
    public void MethodSetup() { /* Prepare for test */ }

    // The actual performance test
    [SailfishMethod]
    public async Task PerformanceTest(CancellationToken ct)
    {
        // Your code under test
    }

    // Cleanup after each iteration
    [SailfishIterationTeardown]
    public void IterationTeardown() { /* Clean up */ }
}
```

## 🔗 Additional Resources

- **[Sailfish Documentation](https://sailfish-docs.com)** - Complete framework documentation
- **[GitHub Repository](https://github.com/paulegradie/Sailfish)** - Source code and issues
- **[NuGet Package](https://www.nuget.org/packages/Sailfish/)** - Latest releases
- **[Performance Testing Guide](https://sailfish-docs.com/guides/performance-testing)** - Best practices and methodologies

---

## 🤝 Contributing

Found an issue or want to add an example? Contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Add your example with proper documentation
4. Submit a pull request

---

**Happy Performance Testing! 🚀**

*Built with ❤️ using [Sailfish](https://github.com/paulegradie/Sailfish)*