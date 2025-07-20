---
title: Getting Started
---

Welcome to Sailfish! This guide will help you get up and running with performance testing in .NET applications.

{% info-callout title="What You'll Learn" %}
In this guide, you'll learn how to set up Sailfish in your project and write your first performance test. We'll cover both test project and library approaches to fit your workflow.
{% /info-callout %}

## 🚀 Choose Your Approach

There are two main ways to use Sailfish in your projects:

{% feature-grid columns=2 %}
  {% feature-card
    title="Test Project"
    description="Run Sailfish tests directly from your IDE like xUnit or NUnit tests. Perfect for getting started quickly."
  /%}

  {% feature-card
    title="Library Integration"
    description="Integrate Sailfish into your existing applications for more advanced scenarios and custom workflows."
  /%}
{% /feature-grid %}

## 🧪 Test Project Approach

{% success-callout title="Recommended for Beginners" %}
The test project approach is the easiest way to get started with Sailfish. It integrates seamlessly with your existing testing workflow and requires minimal setup.
{% /success-callout %}

When you install the [Sailfish TestAdapter](https://www.nuget.org/packages/Sailfish.TestAdapter) NuGet package, you can write and run Sailfish tests directly from the IDE as if it were a test project using xUnit or NUnit. This is a great way to get started with Sailfish and get it integrated into your workflow.

**Key Benefits:**
- **IDE Integration**: Run tests directly from Visual Studio, VS Code, or Rider
- **Familiar Workflow**: Works just like your existing unit tests
- **Easy Debugging**: Set breakpoints and debug performance tests
- **CI/CD Ready**: Integrates with existing test pipelines

## 📚 Library Approach

{% code-callout title="Advanced Integration" %}
The library approach gives you full control over Sailfish execution and is perfect for custom workflows and production monitoring scenarios.
{% /code-callout %}

If you don't need a test project, then your application can install the [Sailfish](https://www.nuget.org/packages/Sailfish) nuget package. The library exposes various tools as well as an entry point into the Sailfish execution program. This entry point requires an IRunSettings, which can be created using the [RunSettingsBuilder](https://github.com/paulegradie/Sailfish/blob/main/source/Sailfish/RunSettingsBuilder.cs).

You can optionally place your tests in a separate project, which installs the test adapter. When your application's main project depends on the test project, you get access to the Sailfish library.

**Use Cases:**
- **Custom Execution Logic**: Build your own test runners
- **Production Monitoring**: Run performance tests in live environments
- **Advanced Reporting**: Create custom output formats
- **Integration Testing**: Embed performance tests in larger workflows

{% note-callout title="Next Steps" %}
Ready to dive deeper? Check out our [Quick Start Guide](/docs/0/quick-start) for hands-on examples, or explore [Installation](/docs/0/installation) for detailed setup instructions.
{% /note-callout %}
