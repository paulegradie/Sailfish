---
title: When should I use Sailfish?
---

Choosing the right performance testing tool is crucial for getting accurate and meaningful results. This guide will help you understand when Sailfish is the best choice for your performance testing needs.

## The Performance Testing Spectrum

{% info-callout title="Understanding Scale" %}
Benchmarking software performance is like measuring objects in the universe. Sometimes you need to measure very quick things (like atoms), and other times you'll need to measure slower things (like stars). The same applies to performance testing.
{% /info-callout %}

Performance testing tools are optimized for different scales:

- **Nanosecond scale**: Addition operations, simple calculations
- **Microsecond scale**: Memory operations, basic algorithms
- **Millisecond scale**: Database queries, API calls, file operations
- **Second scale**: Complex workflows, batch operations

## When to Choose Sailfish

{% success-callout title="Sailfish is Perfect For" %}
Sailfish excels when you need a library that can measure execution time at the **millisecond scale** with statistical rigor and enterprise-grade features.
{% /success-callout %}

### Ideal Use Cases

{% feature-grid columns=2 %}
{% feature-card title="API Performance Testing" description="Measure HTTP requests, database queries, and service calls with statistical analysis." /%}

{% feature-card title="Algorithm Complexity Analysis" description="Use machine learning to determine Big O complexity and predict scaling behavior." /%}

{% feature-card title="Regression Detection" description="Automatically detect performance regressions with before/after statistical testing." /%}

{% feature-card title="Production Monitoring" description="Run performance tests in production environments with minimal overhead." /%}
{% /feature-grid %}

## Key Sailfish Advantages

### 🔧 **Developer-Friendly Integration**

{% tip-callout title="Test Project Experience" %}
Sailfish has a test adapter that makes performance tests behave like NUnit or xUnit tests in your IDE. You can run, debug, and manage them just like unit tests.
{% /tip-callout %}

- **Runs in-process** - Debug your tests without attaching to external processes
- **IDE integration** - Full support for Visual Studio, VS Code, and Rider
- **Familiar workflow** - Works with existing test runners and CI/CD pipelines

### 📊 **Advanced Statistical Analysis**

{% code-callout title="Built-in Intelligence" %}
Sailfish performs sophisticated statistical analysis and predictive modeling, leveraging outlier detection and distribution testing to provide reliable results.
{% /code-callout %}

- **Outlier detection** - Automatically identifies and handles anomalous measurements
- **Distribution analysis** - Uses appropriate statistical tests for your data
- **Complexity estimation** - Machine learning algorithms predict algorithmic complexity

### 🏢 **Enterprise Ready**

{% info-callout title="Production Deployment" %}
Unlike many benchmarking tools, Sailfish is designed to run safely in production environments with configurable overhead and resource management.
{% /info-callout %}

- **Low overhead** - Minimal impact on production systems
- **Configurable execution** - Control sample sizes, iterations, and resource usage
- **Comprehensive reporting** - Multiple output formats for different stakeholders

## When NOT to Use Sailfish

{% warning-callout title="Consider Alternatives" %}
While Sailfish is powerful, it may not be the best choice for every scenario.
{% /warning-callout %}

**Consider other tools for:**

- **Nanosecond-scale benchmarks** - Use BenchmarkDotNet for micro-benchmarks
- **Load testing** - Use k6, JMeter, or NBomber for high-concurrency testing
- **Simple timing** - Use Stopwatch for basic performance measurements
- **Memory profiling** - Use dedicated profilers like dotMemory or PerfView

## Comparison with Alternatives

| Feature | Sailfish | BenchmarkDotNet | NBomber | k6 |
|---------|----------|-----------------|---------|-----|
| **Scale** | Milliseconds | Nanoseconds | Seconds | Seconds |
| **Statistical Analysis** | ✅ Advanced | ✅ Basic | ❌ | ❌ |
| **IDE Integration** | ✅ Full | ✅ Limited | ❌ | ❌ |
| **Production Safe** | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes |
| **ML Complexity Analysis** | ✅ Yes | ❌ No | ❌ No | ❌ No |
| **Regression Detection** | ✅ Automated | ❌ Manual | ❌ Manual | ❌ Manual |

{% note-callout title="The Bottom Line" %}
Choose Sailfish when you need **statistical rigor**, **developer-friendly tooling**, and **enterprise-grade features** for performance testing at the millisecond scale.
{% /note-callout %}
