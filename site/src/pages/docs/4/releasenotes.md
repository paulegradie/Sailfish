---
title: Release Notes
---

Stay up to date with the latest Sailfish features, improvements, and bug fixes. Each release brings new capabilities and enhanced performance testing experiences.

{% info-callout title="Latest Release" %}
Sailfish 2.1.0 is now available with .NET 8 and .NET 9 support, bringing significant performance improvements and modern framework features.
{% /info-callout %}

## 🚀 Version 2.1.0 - Latest

{% warning-callout title="Breaking Change" %}
**Deprecated .NET 6 support** - now supports .NET 8 and .NET 9 only for improved performance and latest features.
{% /warning-callout %}

### ✨ New Features & Improvements

{% feature-grid columns=2 %}
{% feature-card title="Framework Upgrades" description="Updated all projects to target .NET 8.0 and .NET 9.0 for improved performance and latest features." /%}

{% feature-card title="Test Builder Enhancements" description="Improved test builders with enhanced functionality and better error handling." /%}

{% feature-card title="Test Adapter Improvements" description="Enhanced Sailfish.TestAdapter with multi-framework support and improved MSBuild integration." /%}

{% feature-card title="Performance Boost" description="Leveraged .NET 8/9 performance improvements for faster test execution." /%}
{% /feature-grid %}

### 📦 Dependency Updates

- **Microsoft.NET.Test.Sdk** to 17.14.1
- **xunit** to 2.9.3 and **xunit.runner.visualstudio** to 3.1.1
- **NSubstitute** to 5.3.0
- **Shouldly** to 4.3.0
- **Microsoft.Extensions.*** packages to 9.0.7
- **CsvHelper** to 33.1.0
- **Autofac** to 8.3.0
- **MediatR** to 13.0.0

## 🔧 Version 2.0.268 - Memory Management

{% success-callout title="Stability Improvements" %}
Major memory management improvements to prevent data loss during long-running test suites.
{% /success-callout %}

- **Memory Management**: Replaced internal MemoryCache with custom state cache implementation to prevent data eviction issues
- **Bug Fix**: Resolves issue #180 where MemoryCache would unexpectedly evict performance tracking data during long-running test suites
- **Stability**: Improved data persistence and reliability for test execution state management

## 📚 Version 2.0.192 - Code Quality

{% tip-callout title="Developer Experience" %}
Comprehensive code cleanup and documentation improvements for better maintainability.
{% /tip-callout %}

- **Code Quality**: Comprehensive code cleanup and refactoring for improved maintainability
- **Documentation**: Updated and enhanced documentation with clearer examples and better organization
- **Bug Fix**: Fixed stale documentation links in test vs production environment guidance
- **Developer Experience**: Improved code readability and consistency across the codebase

## 🎯 Version 2.0.0 - Major Architecture Update

{% warning-callout title="Breaking Changes" %}
**Breaking Change**: Public mediator contracts converted to records with CamelCased property names for better .NET conventions.
{% /warning-callout %}

### Key Improvements

{% feature-grid columns=2 %}
{% feature-card title="Modern Architecture" description="Improved internal organization with better separation of concerns and cleaner module structure." /%}

{% feature-card title="Enhanced Logging" description="Better formatting, log levels, and colored console output for improved debugging experience." /%}

{% feature-card title="Exception Handling" description="Significantly improved exception handling in test output window with better error reporting." /%}

{% feature-card title="API Modernization" description="Modernized public contracts to use record types for immutability and better performance." /%}
{% /feature-grid %}

## 🛠️ Version 1.6.7 - Reliability Fixes

{% success-callout title="Critical Bug Fixes" %}
Fixed critical issue where subsequent test cases would fail to execute if an earlier test case threw an exception.
{% /success-callout %}

- **Test Execution Reliability**: Fixed critical issue where subsequent test cases would fail to execute if an earlier test case threw an exception
- **Logging Accuracy**: Resolved bug where test case counts were not incremented correctly when exceptions occurred during test execution
- **Exception Recovery**: Improved test runner's ability to continue execution after encountering exceptions in individual test methods
- **Stability**: Enhanced overall test suite reliability when dealing with failing test cases

## 🎨 Version 1.6.4 - IDE Experience

{% tip-callout title="Visual Improvements" %}
Fixed spinner loading bug and improved visual feedback in development environments.
{% /tip-callout %}

- **IDE Integration**: Fixed spinner loading bug in IDE integration where multiple progress spinners would stack when exceptions were thrown
- **User Experience**: Improved visual feedback in development environments during test execution
- **Exception Handling**: Better handling of UI state when test execution encounters errors
- **TestAdapter**: Enhanced test adapter stability for Visual Studio and other IDE integrations

## 📊 Version 1.6.0 - SailDiff Introduction

{% success-callout title="Major Feature Release" %}
Introduction of **SailDiff** - automated before & after statistical testing on performance data.
{% /success-callout %}

### Key Features

{% feature-grid columns=2 %}
{% feature-card title="Statistical Analysis" description="Added T-Test, Mann-Whitney U Test, Kolmogorov-Smirnov Test, and Wilcoxon Signed-Rank Test support." /%}

{% feature-card title="Regression Detection" description="Automated detection of performance improvements and regressions with configurable significance thresholds." /%}

{% feature-card title="Enhanced Tracking" description="Enhanced tracking file system for historical performance data comparison." /%}

{% feature-card title="Configuration Support" description="Added .sailfish.json configuration file support for test projects." /%}
{% /feature-grid %}

## 🧠 Version 1.5.0 - ScaleFish Introduction

{% success-callout title="Machine Learning Analysis" %}
Introduction of **ScaleFish** - machine learning tool for complexity analysis and prediction.
{% /success-callout %}

### Revolutionary Features

{% feature-grid columns=2 %}
{% feature-card title="Complexity Modeling" description="Added support for analyzing algorithmic complexity patterns (O(1), O(n), O(n²), O(log n), O(n!), etc.)" /%}

{% feature-card title="Predictive Analysis" description="Machine learning models to predict performance scaling based on input size." /%}

{% feature-card title="Mathematical Functions" description="Implemented complexity function library (Linear, Quadratic, Logarithmic, Exponential, Factorial)." /%}

{% feature-card title="Performance Modeling" description="Automated curve fitting and complexity estimation for performance test results." /%}
{% /feature-grid %}

## 🧪 Version 1.4.0 - Test Adapter

{% success-callout title="IDE Integration" %}
Introduction of **Sailfish.TestAdapter** - Visual Studio Test Platform integration.
{% /success-callout %}

- **IDE Integration**: Full support for running Sailfish tests directly from Visual Studio, Rider, and other IDEs
- **Test Discovery**: Automatic discovery of classes decorated with `[Sailfish]` attribute
- **Test Execution**: Seamless integration with existing test runners and CI/CD pipelines
- **Progress Reporting**: Real-time test execution feedback in IDE test explorers
- **Multi-Framework Support**: Compatible with multiple .NET framework versions

## 🔍 Version 1.3.0 - Code Analyzers

{% tip-callout title="Developer Experience" %}
Introduction of **Sailfish.Analyzers** - Roslyn code analyzers for compile-time validation.
{% /tip-callout %}

- **Code Analysis**: Static analysis to ensure proper Sailfish attribute usage and test structure
- **Developer Experience**: Compile-time warnings and suggestions for optimal performance test patterns
- **Best Practices**: Automated enforcement of Sailfish coding conventions and patterns
- **IDE Support**: IntelliSense and code completion improvements for Sailfish attributes

## 📄 Version 1.2.0 - Output Formats

{% code-callout title="Comprehensive Reporting" %}
Added comprehensive output format support (Markdown, CSV, JSON) with new attributes for automatic report generation.
{% /code-callout %}

- **Output Formats**: Added comprehensive output format support (Markdown, CSV, JSON)
- **WriteToMarkdown**: New attribute for automatic markdown report generation
- **WriteToCsv**: New attribute for CSV data export functionality
- **File Management**: Enhanced tracking file organization and management
- **Extensibility**: Improved notification handler system for custom output formats
- **Documentation**: Automated generation of performance test documentation

## 🔢 Version 1.1.0 - Variable System

{% success-callout title="Parameterized Testing" %}
Introduction of `[SailfishVariable]` and `[SailfishRangeVariable]` attributes for comprehensive parameterized testing.
{% /success-callout %}

- **Variable System**: Introduction of `[SailfishVariable]` and `[SailfishRangeVariable]` attributes
- **Parameterized Testing**: Support for multiple test cases with different input parameters
- **Test Case Generation**: Automatic generation of test case combinations from variable definitions
- **Range Variables**: Support for generating sequential test parameters with start, count, and step values
- **State Management**: Enhanced test class state management across variable combinations

## 🎉 Version 1.0.0 - Initial Release

{% success-callout title="Foundation Release" %}
Core Sailfish performance testing framework with comprehensive features for enterprise-grade performance testing.
{% /success-callout %}

### Core Features

{% feature-grid columns=2 %}
{% feature-card title="Test Lifecycle" description="Complete test lifecycle with setup/teardown methods for comprehensive test control." /%}

{% feature-card title="Performance Measurement" description="High-precision timing and statistical analysis with outlier detection." /%}

{% feature-card title="Attribute System" description="Core attribute system ([Sailfish], [SailfishMethod]) for easy test definition." /%}

{% feature-card title="Dependency Injection" description="Autofac-based dependency injection container for flexible test setup." /%}
{% /feature-grid %}

### Technical Foundation

- **Logging System**: Configurable logging with multiple log levels and colored console output
- **MediatR Integration**: Event-driven architecture using MediatR for internal communication
- **Statistical Analysis**: Basic descriptive statistics (mean, median, standard deviation, variance)
- **Outlier Detection**: Built-in outlier detection and removal capabilities
- **Overhead Estimation**: Performance overhead estimation and compensation

{% note-callout title="Journey Complete" %}
From the initial 1.0.0 release to the latest 2.1.0, Sailfish has evolved into a comprehensive performance testing platform with machine learning capabilities, statistical analysis, and enterprise-grade features. Thank you for being part of this journey!
{% /note-callout %}
