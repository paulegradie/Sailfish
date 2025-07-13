---
title: Release Notes
---

- 2.1.0
  - **Breaking Change**: Deprecated .NET 6 support - now supports .NET 8 and .NET 9 only
  - **Framework Upgrades**: Updated all projects to target .NET 8.0 and .NET 9.0 for improved performance and latest features
  - **Test Builder Enhancements**: Improved test builders with enhanced functionality and better error handling
  - **Dependency Updates**: Updated major dependencies including:
    - Microsoft.NET.Test.Sdk to 17.14.1
    - xunit to 2.9.3 and xunit.runner.visualstudio to 3.1.1
    - NSubstitute to 5.3.0
    - Shouldly to 4.3.0
    - Microsoft.Extensions.* packages to 9.0.7
    - CsvHelper to 33.1.0
    - Autofac to 8.3.0
    - MediatR to 13.0.0
  - **Test Adapter Improvements**: Enhanced Sailfish.TestAdapter with multi-framework support and improved MSBuild integration
  - **Package Management**: Implemented framework-specific package references for better compatibility
  - **Build System**: Updated build targets and props files for improved NuGet package integration
  - **Performance**: Leveraged .NET 8/9 performance improvements for faster test execution

- 2.0.268
  - **Memory Management**: Replaced internal MemoryCache with custom state cache implementation to prevent data eviction issues
  - **Bug Fix**: Resolves issue #180 where MemoryCache would unexpectedly evict performance tracking data during long-running test suites
  - **Stability**: Improved data persistence and reliability for test execution state management

- 2.0.192
  - **Code Quality**: Comprehensive code cleanup and refactoring for improved maintainability
  - **Documentation**: Updated and enhanced documentation with clearer examples and better organization
  - **Bug Fix**: Fixed stale documentation links in test vs production environment guidance
  - **Developer Experience**: Improved code readability and consistency across the codebase

- 2.0.0
  - **Breaking Change**: Public mediator contracts converted to records with CamelCased property names for better .NET conventions
  - **Architecture**: Improved internal organization with better separation of concerns and cleaner module structure
  - **Logging System**: Enhanced logging capabilities with better formatting, log levels, and colored console output
  - **Exception Handling**: Significantly improved exception handling in test output window with better error reporting and stack trace display
  - **Performance**: Optimized internal communication patterns using MediatR for better testability and maintainability
  - **API Design**: Modernized public contracts to use record types for immutability and better performance

- 1.6.7
  - **Test Execution Reliability**: Fixed critical issue where subsequent test cases would fail to execute if an earlier test case threw an exception
  - **Logging Accuracy**: Resolved bug where test case counts were not incremented correctly when exceptions occurred during test execution
  - **Exception Recovery**: Improved test runner's ability to continue execution after encountering exceptions in individual test methods
  - **Stability**: Enhanced overall test suite reliability when dealing with failing test cases

- 1.6.4
  - **IDE Integration**: Fixed spinner loading bug in IDE integration where multiple progress spinners would stack when exceptions were thrown
  - **User Experience**: Improved visual feedback in development environments during test execution
  - **Exception Handling**: Better handling of UI state when test execution encounters errors
  - **TestAdapter**: Enhanced test adapter stability for Visual Studio and other IDE integrations

- 1.6.0
  - **Major Feature**: Introduction of **SailDiff** - automated before & after statistical testing on performance data
  - **Statistical Analysis**: Added T-Test, Mann-Whitney U Test, Kolmogorov-Smirnov Test, and Wilcoxon Signed-Rank Test support
  - **Performance Regression Detection**: Automated detection of performance improvements and regressions with configurable significance thresholds
  - **Tracking Files**: Enhanced tracking file system for historical performance data comparison
  - **Configuration**: Added `.sailfish.json` configuration file support for test projects
  - **Reporting**: Statistical test results displayed in console, IDE output, and markdown formats

- 1.5.0
  - **Major Feature**: Introduction of **ScaleFish** - machine learning tool for complexity analysis and prediction
  - **Complexity Modeling**: Added support for analyzing algorithmic complexity patterns (O(1), O(n), O(n²), O(log n), O(n!), etc.)
  - **Predictive Analysis**: Machine learning models to predict performance scaling based on input size
  - **SailfishVariable Enhancement**: Added complexity analysis support to variable attributes
  - **Mathematical Functions**: Implemented complexity function library (Linear, Quadratic, Logarithmic, Exponential, Factorial)
  - **Performance Modeling**: Automated curve fitting and complexity estimation for performance test results

- 1.4.0
  - **Major Feature**: Introduction of **Sailfish.TestAdapter** - Visual Studio Test Platform integration
  - **IDE Integration**: Full support for running Sailfish tests directly from Visual Studio, Rider, and other IDEs
  - **Test Discovery**: Automatic discovery of classes decorated with `[Sailfish]` attribute
  - **Test Execution**: Seamless integration with existing test runners and CI/CD pipelines
  - **Progress Reporting**: Real-time test execution feedback in IDE test explorers
  - **Multi-Framework Support**: Compatible with multiple .NET framework versions

- 1.3.0
  - **Major Feature**: Introduction of **Sailfish.Analyzers** - Roslyn code analyzers for compile-time validation
  - **Code Analysis**: Static analysis to ensure proper Sailfish attribute usage and test structure
  - **Developer Experience**: Compile-time warnings and suggestions for optimal performance test patterns
  - **Best Practices**: Automated enforcement of Sailfish coding conventions and patterns
  - **IDE Support**: IntelliSense and code completion improvements for Sailfish attributes

- 1.2.0
  - **Output Formats**: Added comprehensive output format support (Markdown, CSV, JSON)
  - **WriteToMarkdown**: New attribute for automatic markdown report generation
  - **WriteToCsv**: New attribute for CSV data export functionality
  - **File Management**: Enhanced tracking file organization and management
  - **Extensibility**: Improved notification handler system for custom output formats
  - **Documentation**: Automated generation of performance test documentation

- 1.1.0
  - **Variable System**: Introduction of `[SailfishVariable]` and `[SailfishRangeVariable]` attributes
  - **Parameterized Testing**: Support for multiple test cases with different input parameters
  - **Test Case Generation**: Automatic generation of test case combinations from variable definitions
  - **Range Variables**: Support for generating sequential test parameters with start, count, and step values
  - **State Management**: Enhanced test class state management across variable combinations

- 1.0.0
  - **Initial Release**: Core Sailfish performance testing framework
  - **Test Lifecycle**: Complete test lifecycle with setup/teardown methods (`[SailfishGlobalSetup]`, `[SailfishMethodSetup]`, `[SailfishIterationSetup]`, etc.)
  - **Performance Measurement**: High-precision timing and statistical analysis
  - **Attribute System**: Core attribute system (`[Sailfish]`, `[SailfishMethod]`)
  - **Logging System**: Configurable logging with multiple log levels and colored console output
  - **Dependency Injection**: Autofac-based dependency injection container
  - **MediatR Integration**: Event-driven architecture using MediatR for internal communication
  - **Statistical Analysis**: Basic descriptive statistics (mean, median, standard deviation, variance)
  - **Outlier Detection**: Built-in outlier detection and removal capabilities
  - **Overhead Estimation**: Performance overhead estimation and compensation
