---
title: Quick Start Guide
---

Get up and running with Sailfish in just a few minutes! This guide will walk you through creating your first performance test.

{% info-callout title="Prerequisites" %}
Before you begin, make sure you have:
- .NET 6.0 or later installed
- A C# development environment (Visual Studio, VS Code, or Rider)
- Basic familiarity with C# and unit testing
{% /info-callout %}

## 🚀 Step-by-Step Setup

### 1️⃣ Create a Test Project

{% success-callout title="Quick Setup" %}
Create a new class library project and install the Sailfish Test Adapter for full IDE integration.
{% /success-callout %}

```bash
# Create a new test project
dotnet new classlib -n MyApp.PerformanceTests
cd MyApp.PerformanceTests

# Install the Sailfish Test Adapter
dotnet add package Sailfish.TestAdapter
```

{% tip-callout title="IDE Integration" %}
The Test Adapter allows you to run Sailfish tests directly from your IDE, just like xUnit or NUnit tests.
{% /tip-callout %}

### 2️⃣ Write Your First Performance Test

{% code-callout title="Simple and Powerful" %}
Create a new test class with the `[Sailfish]` attribute - it's that easy to get started!
{% /code-callout %}

```csharp
using Sailfish.Attributes;

[Sailfish]
public class Example
{
    private readonly IClient client;

    [SailfishVariable(1, 10)]
    public int N { get; set; }

    public Example(IClient client)
    {
        this.client = client;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await client.Get("/api", ct);
    }
}
```

{% info-callout title="Understanding the Code" %}
- `[Sailfish]` marks the class as a performance test
- `[SailfishVariable]` creates test variations with different values
- `[SailfishMethod]` marks the method to be performance tested
{% /info-callout %}

### 3️⃣ Register Dependencies (Optional)

{% tip-callout title="Dependency Injection" %}
If your tests need dependencies, implement `IProvideARegistrationCallback` to register them with the container.
{% /tip-callout %}

```csharp
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder,
        CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.Register(_ => typeInstance).As<IClient>();
    }
}
```

### 4️⃣ Run Your Performance Tests

{% success-callout title="Multiple Ways to Run" %}
Execute your tests using your preferred test runner - command line, IDE, or CI/CD pipeline.
{% /success-callout %}

```bash
# Run tests from command line
dotnet test

# Or run from your IDE's test explorer
```

### 5️⃣ View Your Results

{% info-callout title="Rich Output Formats" %}
Sailfish provides statistical analysis including mean execution time, standard deviation, and outlier detection across multiple output formats.
{% /info-callout %}

**Output Options:**
- **Console Output**: Immediate feedback in your terminal
- **Tracking Files**: Detailed data for analysis and comparison
- **Custom Formats**: Export to CSV, Markdown, or custom formats

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

Outliers Removed (0)
--------------------

Adjusted Distribution (ms)
--------------------------
119.6471, 105.9743, 107.8113
```

## 🎯 Next Steps

{% feature-grid columns=3 %}
{% feature-card title="Learn the Basics" description="Understand Sailfish attributes, variables, and test lifecycle." /%}

{% feature-card title="Explore Features" description="Discover SailDiff for regression testing and ScaleFish for complexity analysis." /%}

{% feature-card title="Advanced Usage" description="Learn about extensibility, custom handlers, and enterprise features." /%}
{% /feature-grid %}

{% note-callout title="What's Next?" %}
Now that you have your first performance test running, explore our [Sailfish Basics](/docs/1/required-attributes) section to learn more about variables, test lifecycle, and advanced features.
{% /note-callout %}
