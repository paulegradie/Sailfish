# Sailfish Test Adapter

## Overview

The Sailfish Test Adapter is a Visual Studio Test Platform extension that enables discovery and execution of Sailfish performance tests within standard .NET testing environments. This adapter bridges the gap between the Sailfish performance testing framework and IDEs like Visual Studio, JetBrains Rider, and command-line test runners.

## Purpose and Role

This test adapter serves as the integration layer that allows Sailfish performance tests to be:
- **Discovered** by test explorers in IDEs
- **Executed** through standard test runners (`dotnet test`, IDE test runners)
- **Reported** with results displayed in familiar test result windows
- **Integrated** into CI/CD pipelines using standard .NET testing tools

The adapter implements the Visual Studio Test Platform extensibility interfaces to provide seamless integration with the broader .NET testing ecosystem.

## Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   VS Test       │    │  Sailfish Test   │    │    Sailfish        │
│   Platform      │◄──►│     Adapter      │◄──►│   Framework         │
│                 │    │                  │    │                     │
└─────────────────┘    └──────────────────┘    └─────────────────────┘
        │                        │                        │
        │                        │                        │
    ┌───▼────┐              ┌────▼────┐              ┌────▼────┐
    │  IDE   │              │ Test    │              │ Perf    │
    │ Test   │              │ Cases   │              │ Test    │
    │Explorer│              │         │              │ Logic   │
    └────────┘              └─────────┘              └─────────┘
```

### Data Flow
1. **Discovery Phase**: TestDiscoverer scans assemblies for `[SailfishAttribute]` classes
2. **Test Case Creation**: Discovered classes are converted to VS Test Platform TestCase objects
3. **Execution Phase**: TestExecutor receives TestCases and delegates to Sailfish execution engine
4. **Result Reporting**: Test results are reported back through VS Test Platform interfaces
5. **IDE Integration**: Results appear in test explorers and output windows

## Core Components

### TestDiscoverer (`TestDiscoverer.cs`)
**Interface**: `ITestDiscoverer`
**Purpose**: Discovers Sailfish performance tests in compiled assemblies

**Key Responsibilities**:
- Scans `.dll` files for classes decorated with `[SailfishAttribute]`
- Filters out excluded assemblies (test adapter itself, etc.)
- Creates `TestCase` objects for each discovered test method
- Handles discovery errors and reports them to the test platform

**Discovery Process**:
1. Receives list of source assemblies from VS Test Platform
2. Filters assemblies using exclusion list
3. Uses `TestDiscovery` service to scan for Sailfish test classes
4. Converts discovered tests to `TestCase` objects with metadata
5. Reports test cases back to discovery sink

### TestExecutor (`TestExecutor.cs`)
**Interface**: `ITestExecutor`
**Purpose**: Executes discovered Sailfish tests and reports results

**Key Responsibilities**:
- Receives test cases from VS Test Platform
- Sets up Autofac dependency injection container
- Delegates execution to Sailfish execution engine
- Handles test cancellation and cleanup
- Reports test results and errors back to framework

**Execution Process**:
1. Loads run settings and configures DI container
2. Registers Sailfish types and test adapter services
3. Resolves execution engine and runs tests
4. Handles exceptions and reports skipped tests on failure
5. Ensures proper container disposal

### Discovery Subsystem (`Discovery/`)
**Core Service**: `TestDiscovery`
**Purpose**: Low-level test discovery logic and metadata extraction

**Key Components**:
- `TestDiscovery.cs` - Main discovery orchestration
- `DiscoveryAnalysisMethods.cs` - Assembly analysis and type loading
- `TestCaseItemCreator.cs` - TestCase object creation with metadata
- `ClassMetaData.cs` / `MethodMetaData.cs` - Test metadata containers
- `TypeLoader.cs` - Safe assembly loading and type resolution

**Discovery Features**:
- Project file detection for context
- Assembly loading with error handling
- Sailfish attribute detection and validation
- Test case metadata extraction and hashing
- Support for complex test scenarios

### Execution Subsystem (`Execution/`)
**Core Service**: `TestExecution`
**Purpose**: Orchestrates Sailfish test execution within test adapter context

**Key Components**:
- `TestExecution.cs` - Main execution coordinator
- `TestAdapterExecutionProgram.cs` - High-level execution program
- `TestAdapterExecutionEngine.cs` - Core execution engine
- `AdapterSailDiff.cs` / `AdapterScaleFish.cs` - Analysis adapters
- `AdapterRunSettingsLoader.cs` - Configuration loading

**Execution Features**:
- Integration with Sailfish execution pipeline
- MediatR-based event handling and notifications
- Test result formatting and reporting
- Error handling and test skipping
- Cancellation token support

### Dependency Injection (`Registrations/`)
**Core Service**: `TestAdapterRegistrations`
**Purpose**: Configures Autofac container for test adapter services

**Registered Services**:
- Test execution components
- Analysis services (SailDiff, ScaleFish)
- Notification handlers for test events
- Display and formatting services
- Framework integration services

### Test Properties (`TestProperties/`)
**Purpose**: Manages test case metadata and properties

**Key Components**:
- `SailfishManagedProperty.cs` - Defines test case properties
- `PropertyValueExtensionMethods.cs` - Property access helpers

**Properties Managed**:
- Sailfish type information
- Test method metadata
- Execution context data

## Integration Points

### Visual Studio Test Platform Integration
- **Interfaces**: `ITestDiscoverer`, `ITestExecutor`
- **Attributes**: `[FileExtension(".dll")]`, `[DefaultExecutorUri]`, `[ExtensionUri]`
- **Framework Handle**: Receives `IFrameworkHandle` for result reporting
- **Discovery Context**: Uses `IDiscoveryContext` for discovery settings
- **Message Logging**: Integrates with `IMessageLogger` for diagnostic output

### Sailfish Framework Integration
- **Project Reference**: Direct reference to `Sailfish.csproj`
- **Shared Services**: Uses Sailfish DI registration system
- **Execution Engine**: Delegates to Sailfish execution pipeline
- **Analysis Integration**: Wraps SailDiff and ScaleFish analyzers
- **Notification System**: Extends Sailfish MediatR notifications

### NuGet Package Structure
**Package ID**: `Sailfish.TestAdapter`
**Target Frameworks**: .NET 8.0, .NET 9.0

**Package Contents**:
```
lib/
├── net8.0/
│   ├── Sailfish.TestAdapter.dll
│   └── Sailfish.TestAdapter.pdb
└── net9.0/
    ├── Sailfish.TestAdapter.dll
    └── Sailfish.TestAdapter.pdb
build/
├── Sailfish.TestAdapter.props
└── Sailfish.TestAdapter.targets
```

**Build Integration** (`build/`):
- `Sailfish.TestAdapter.props` - MSBuild properties (currently minimal)
- `Sailfish.TestAdapter.targets` - Copies adapter DLL to output directory and configures TestAdapterPath

**Auto-Discovery**: The targets file ensures the test adapter is automatically discovered by VS Test Platform when the NuGet package is referenced.

## Development Guidelines

### Modifying the Adapter

1. **Discovery Changes**: Modify `Discovery/` components for test finding logic
2. **Execution Changes**: Modify `Execution/` components for test running logic
3. **Integration Changes**: Modify main `TestDiscoverer.cs` / `TestExecutor.cs` files
4. **DI Changes**: Update `TestAdapterRegistrations.cs` for new services

### Testing the Adapter

**Unit Testing**: See `Tests.TestAdapter` project for adapter-specific tests

**Integration Testing**:
1. Build the adapter project
2. Reference it in a test project with Sailfish tests
3. Run tests through IDE or `dotnet test`
4. Verify discovery and execution work correctly

**Debugging Tips**:
- Use `IMessageLogger` for diagnostic output during discovery/execution
- Check VS Test Platform output window for adapter messages
- Verify NuGet package structure if distribution issues occur
- Test with both project references and NuGet package references

### Common Issues

**Adapter Not Discovered**:
- Verify NuGet package is properly referenced
- Check that `Sailfish.TestAdapter.targets` is being imported
- Ensure output directory contains adapter DLL

**Tests Not Found**:
- Verify classes have `[SailfishAttribute]`
- Check that assemblies aren't in exclusion list
- Review discovery error messages in test output

**Execution Failures**:
- Check for missing dependencies in test project
- Verify Sailfish framework is properly configured
- Review exception messages in test results

## Current State Tracking

### Implemented Features ✅
- **Test Discovery**: Full support for Sailfish attribute-based discovery
- **Test Execution**: Complete integration with Sailfish execution engine
- **Result Reporting**: VS Test Platform result integration
- **Error Handling**: Comprehensive exception handling and reporting
- **NuGet Packaging**: Proper test adapter packaging and auto-discovery
- **Multi-Framework**: Support for .NET 8.0 and .NET 9.0
- **IDE Integration**: Works with Visual Studio, Rider, and command-line tools
- **Cancellation**: Test execution cancellation support
- **DI Integration**: Full Autofac container integration
- **Notification System**: MediatR-based event handling

### Known Limitations ⚠️
- **Execution Environments**: Limited compared to MSTest (no remote execution, etc.)
- **Test Filtering**: Basic filtering support (may need enhancement)
- **Parallel Execution**: Inherits limitations from Sailfish framework
- **Configuration**: Limited run-time configuration options

### Recent Changes 📝
- Updated to support .NET 9.0 alongside .NET 8.0
- Enhanced error handling in discovery and execution phases
- Improved NuGet package structure for better auto-discovery
- Added comprehensive notification handling system
- Integrated with latest Sailfish framework features

### In Progress: Queue Migration 🚧
- **Queue Message Contract**: ✅ Completed (Task 1)
- **Intercepting Queue Architecture**: 🔄 In Progress
- **Batch Processing System**: 📋 Planned
- **Cross-Test-Case Analysis**: 📋 Planned

> **Note for AI Agents**: This section should be updated whenever changes are made to the test adapter. Include date, description of changes, and any breaking changes or migration notes.

## Queue Migration Architecture

### **NEW: Intercepting Queue Architecture** 🎯

#### Current Architecture (Being Replaced)
The test adapter currently follows a direct framework publishing model:

```
TestExecutor → SailfishExecutionEngine → TestCaseCompletedNotification
                                              ↓
                                    TestCaseCompletedNotificationHandler
                                              ↓
                                    FrameworkTestCaseEndNotification
                                              ↓
                                    FrameworkTestCaseEndNotificationHandler
                                              ↓
                                        VS Test Platform
```

#### **NEW: Intercepting Queue Architecture** 🎯
Introduce a queue system that **intercepts** framework publishing to enable batch processing and cross-test-case analysis:

```
TestExecutor → SailfishExecutionEngine → TestCaseCompletedNotification
                                              ↓
                                    TestCaseCompletedNotificationHandler
                                              ↓
                                    TestCompletionQueueMessage → In-Memory Queue
                                              ↓
                                    Queue Processors (Batching, Comparison, Analysis)
                                              ↓
                                    Enhanced FrameworkTestCaseEndNotification(s)
                                              ↓
                                    FrameworkTestCaseEndNotificationHandler
                                              ↓
                                        VS Test Platform
```

#### **Key Architectural Changes**:
- **NO direct framework publishing** from TestCaseCompletedNotificationHandler
- **Queue processors are responsible** for publishing FrameworkTestCaseEndNotification
- **Batching and cross-test-case analysis** occurs before framework reporting
- **Enhanced results** with comparison data sent to framework

#### Queue Infrastructure Components

**Core Queue System**:
- **In-Memory Message Queue**: Lightweight queue using System.Threading.Channels
- **Test Case Batching Service**: Groups related test cases for batch processing
- **Queue Processors**: Process batched results and publish to framework
- **Message Contracts**: Standardized message formats for test completion data

**Batch Processing**:
- **Batch Completion Detection**: Determines when test case groups are complete
- **Cross-Test-Case Analysis**: Compares and analyzes multiple test cases
- **Framework Publishing Processor**: Publishes enhanced results to VS Test Platform
- **Timeout Handling**: Processes incomplete batches after timeout

**Integration Points**:
- **Modified Notification Handler**: TestCaseCompletedNotificationHandler publishes to queue instead of framework
- **Container Integration**: Register all queue services in Autofac container
- **Lifecycle Management**: Ensure proper queue startup/shutdown with test execution
- **Fallback Mechanism**: Direct framework publishing if queue fails

#### Implementation Status

**✅ Completed Components**:

**1. Queue Message Contract** (Task 1):
```csharp
public class TestCompletionQueueMessage
{
    public string TestCaseId { get; set; }
    public TestExecutionResult TestResult { get; set; }
    public DateTime CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public PerformanceMetrics PerformanceMetrics { get; set; }
}
```

**🔄 In Progress Components**:

**2. Queue Publisher Interface** (Task 2 - Current):
```csharp
public interface ITestCompletionQueuePublisher
{
    Task PublishTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken);
}
```

**📋 Planned Components**:

**3. Batching Service Interface** (Task 15):
```csharp
public interface ITestCaseBatchingService
{
    Task AddTestCaseToBatch(TestCompletionQueueMessage message);
    Task<bool> IsBatchComplete(string batchId);
}
```

**4. Framework Publishing Processor** (Task 17):
```csharp
public class FrameworkPublishingProcessor : ITestCompletionQueueProcessor
{
    Task ProcessTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken);
}
```

#### Benefits of Intercepting Queue Architecture

**Cross-Test-Case Analysis** 🎯:
- **Batch Processing**: Group related test cases for comparison analysis
- **Performance Comparison**: Compare multiple test methods within a single test run
- **Baseline Analysis**: Establish performance baselines and detect regressions
- **Statistical Analysis**: Advanced statistical analysis across test case groups
- **Enhanced Results**: Enrich test results with comparison data before framework reporting

**Flexible Result Processing**:
- **Batch Completion Detection**: Wait for all related tests before processing
- **Timeout Handling**: Process incomplete batches after configurable timeouts
- **Result Enhancement**: Add comparison rankings, performance insights, and analysis
- **Custom Processors**: Extensible processor pipeline for custom analysis

**Framework Integration**:
- **Seamless IDE Experience**: Enhanced results appear normally in test explorers
- **No Breaking Changes**: Maintains existing VS Test Platform contracts
- **Fallback Support**: Direct framework publishing if queue processing fails
- **Configurable**: Enable/disable queue processing via configuration

**Extensible Post-Processing**:
- **Historical Data Collection**: Store test results for trend analysis
- **Performance Analysis**: Deep analysis of performance metrics across batches
- **Report Generation**: Automated report creation with comparison data
- **Integration Services**: Push enhanced results to external systems
- **Alerting**: Automated notifications for performance regressions across test groups

#### Implementation Considerations

**Queue Technology**:
- **In-Memory Only**: Implementation using `System.Threading.Channels` within test adapter process
- **Runtime Scoped**: Queue exists only during test execution - no persistence across test runs
- **Process Bound**: All queue operations occur within the test adapter runtime

**Batching Strategy**:
- **Grouping Logic**: Group test cases by comparison attributes, test class, or custom criteria
- **Completion Detection**: Determine when all expected test cases in a batch have been received
- **Timeout Handling**: Process incomplete batches after configurable timeout periods
- **Dynamic Sizing**: Support variable batch sizes based on test discovery

**Framework Integration**:
- **Intercepting Architecture**: Replace direct framework publishing with queue-first processing
- **Enhanced Results**: Enrich test results with comparison data before framework reporting
- **Timing Considerations**: Ensure framework receives results in reasonable time
- **Error Handling**: Robust fallback to direct publishing if queue processing fails

**Container Integration**:
- Register all queue services in `TestAdapterRegistrations`
- Include batching services and framework publishing processors
- Manage queue lifecycle with test execution container
- Ensure proper disposal and cleanup

**Configuration**:
- Queue and batching settings in Sailfish run settings
- Processor configuration and enablement
- Timeout and batch size parameters
- Fallback configuration options

**Backward Compatibility**:
- **CRITICAL**: Fallback to direct framework publishing if queue fails
- Configuration flag to enable/disable queue interception
- Maintain existing notification system for fallback scenarios
- Graceful degradation ensures tests never hang

#### Migration Path

**Phase 1**: Core Infrastructure (Tasks 1-18)
- ✅ Queue message contracts and interfaces
- 🔄 Test case batching and grouping services
- 📋 Framework publishing queue processor
- 📋 In-memory queue implementation using System.Threading.Channels

**Phase 2**: Framework Integration (Tasks 19-30)
- 📋 **CRITICAL**: Replace direct framework publishing with queue interception
- 📋 Integrate queue services with DI container
- 📋 Add fallback mechanisms and error handling
- 📋 Queue lifecycle management with test execution

**Phase 3**: Batch Processing (Tasks 31-42)
- 📋 Cross-test-case comparison and analysis processors
- 📋 Batch completion detection and timeout handling
- 📋 Enhanced result generation with comparison data
- 📋 Processor pipeline and registry system

**Phase 4**: Configuration & Settings (Tasks 43-52)
- 📋 Comprehensive configuration system for queue and batching
- 📋 Runtime configuration updates and validation
- 📋 Documentation and schema support

**Phase 5**: Advanced Features (Tasks 53-62)
- 📋 Performance optimization and memory management
- 📋 Advanced error handling and circuit breaker patterns
- 📋 Comprehensive monitoring and observability
- 📋 Cross-test-case analysis engine

**Current Status**: Task 1 completed ✅, Task 2 in progress 🔄

This intercepting queue architecture will enable powerful cross-test-case analysis and comparison capabilities while maintaining full backward compatibility with the existing VS Test Platform integration.

---

## References

For extensive documentation on the VS Test Framework and adapter development:
- [VS Test Platform Adapter Extensibility](https://github.com/microsoft/vstest/blob/main/docs/RFCs/0004-Adapter-Extensibility.md)
- [On .NET: Exploring the Visual Studio Test Platform](https://docs.microsoft.com/en-us/shows/on-net/exploring-the-visual-studio-test-platform#time=22m24s)

---

**Document Version**: 1.0
**Last Updated**: 2025-01-27
**Maintainer**: AI Agent Documentation System
