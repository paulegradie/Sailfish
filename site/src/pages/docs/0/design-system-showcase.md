---
title: Design System Showcase
---

# Design System Showcase

This page demonstrates all the enhanced documentation components and design system features available in the Sailfish documentation.

## Enhanced Typography

The typography system now uses our comprehensive design tokens with improved readability, better line heights, and consistent spacing. Headers have proper visual hierarchy and the body text uses optimal line lengths for reading.

### Subheading Example

This is a paragraph with enhanced styling. Notice the improved contrast, spacing, and overall readability. The text uses our design system colors and maintains consistency across light and dark themes.

## Callouts and Alerts

### Information Callout

{% info-callout title="Getting Started" %}
This is an information callout that provides helpful context or additional details about a topic. It uses our design system colors and has proper contrast for accessibility.
{% /info-callout %}

### Warning Callout

{% warning-callout title="Important Notice" %}
This is a warning callout that alerts users to important information they should be aware of. It stands out visually while maintaining good readability.
{% /warning-callout %}

### Success Callout

{% success-callout title="Well Done!" %}
This is a success callout that confirms positive actions or outcomes. It provides encouraging feedback to users.
{% /success-callout %}

### Error Callout

{% error-callout title="Something Went Wrong" %}
This is an error callout that alerts users to problems or issues that need attention. It's clearly visible but not overwhelming.
{% /error-callout %}

### Tip Callout

{% tip-callout title="Pro Tip" %}
This is a tip callout that provides helpful suggestions or best practices. It's designed to be friendly and encouraging.
{% /tip-callout %}

### Code Callout

{% code-callout title="Technical Note" %}
This is a code-specific callout with a dark theme that's perfect for technical information, API details, or code-related notes.
{% /code-callout %}

## Step-by-Step Instructions

{% steps %}
  {% step title="Install the Package" %}
    First, install the Sailfish NuGet package in your test project:

    ```bash
    dotnet add package Sailfish.TestAdapter
    ```
  {% /step %}

  {% step title="Create Your First Test" %}
    Create a new test class with the `[Sailfish]` attribute:

    ```csharp
    [Sailfish]
    public class MyPerformanceTest
    {
        [SailfishMethod]
        public void TestMethod()
        {
            // Your performance test code here
        }
    }
    ```
  {% /step %}

  {% step title="Run the Tests" %}
    Execute your tests using your preferred test runner:

    ```bash
    dotnet test
    ```
  {% /step %}
{% /steps %}

## Code Examples

### Basic C# Example

```csharp
[Sailfish]
public class ExampleTest
{
    private readonly IClient client;

    [SailfishVariable(1, 10, 100)]
    public int RequestCount { get; set; }

    public ExampleTest(IClient client)
    {
        this.client = client;
    }

    [SailfishMethod]
    public async Task PerformanceTest(CancellationToken ct)
    {
        for (int i = 0; i < RequestCount; i++)
        {
            await client.GetAsync("/api/data", ct);
        }
    }
}
```

### Configuration Example

```json
{
  "Sailfish": {
    "RunSettings": {
      "NumWarmupIterations": 5,
      "SampleSizeOverride": 30,
      "DisableOverheadEstimation": false
    },
    "AnalysisSettings": {
      "Alpha": 0.05,
      "OutlierDetection": true
    }
  }
}
```

## Feature Grid

{% feature-grid columns=3 %}
  {% feature-card
    title="Performance Testing"
    description="Comprehensive performance testing with statistical analysis and regression detection."
  /%}

  {% feature-card
    title="Machine Learning"
    description="Built-in ML algorithms for intelligent performance analysis and insights."
  /%}

  {% feature-card
    title="Enterprise Ready"
    description="Scalable solutions for teams with advanced reporting and integration capabilities."
  /%}
{% /feature-grid %}

## Tables

| Feature | Open Source | Enterprise |
|---------|-------------|------------|
| Basic Performance Testing | ✅ | ✅ |
| Statistical Analysis | ✅ | ✅ |
| Machine Learning Insights | ❌ | ✅ |
| Advanced Reporting | ❌ | ✅ |
| Priority Support | ❌ | ✅ |

## Lists

### Unordered List
- Enhanced typography with design tokens
- Comprehensive component library
- Smooth animations and transitions
- Improved accessibility features
- Dark mode support

### Ordered List
1. **Plan** - Define your performance testing strategy
2. **Implement** - Write your Sailfish tests
3. **Execute** - Run tests and collect data
4. **Analyze** - Review results and identify issues
5. **Optimize** - Make improvements based on insights

## Blockquotes

> "Sailfish has transformed how we approach performance testing. The statistical analysis and ML insights have helped us catch regressions early and optimize our applications effectively."
> 
> — Senior Developer at Fortune 500 Company

## Links and Navigation

Check out our [Getting Started Guide](/docs/0/getting-started) to begin your journey with Sailfish, or explore the [API Reference](/docs/2/sailfish) for detailed technical information.

For enterprise features and pricing, visit our [Enterprise page](/enterprise) or [contact our sales team](/contact).

---

This showcase demonstrates the enhanced documentation system with improved typography, interactive components, and consistent design language throughout the Sailfish documentation.
