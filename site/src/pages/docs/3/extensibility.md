---
title: Extensibility
---

Sailfish provides extensive extensibility through MediatR commands and notifications, allowing you to customize behavior, integrate with external systems, and create custom workflows.

{% success-callout title="MediatR Integration" %}
Sailfish exposes several public MediatR commands and notifications. Implement MediatR handlers for these to customize Sailfish behavior and integrate with your existing systems.
{% /success-callout %}

## 🔧 Extension Categories

{% feature-grid columns=3 %}
{% feature-card title="Data Sources" description="Customize where tracking data is read from and written to." /%}

{% feature-card title="Notifications" description="React to test lifecycle events for logging, monitoring, or custom processing." /%}

{% feature-card title="Analysis" description="Extend SailDiff and ScaleFish with custom analysis and reporting." /%}
{% /feature-grid %}

## 📁 Data Source Customization

### BeforeAndAfterFileLocationRequest

{% tip-callout title="Custom Data Sources" %}
**Default handler implemented** - Customize this to read tracking data from cloud storage, databases, or other external sources.
{% /tip-callout %}

Used to provide tracking file location data to the statistical test executor. Perfect for reading tracking data from blob storage, databases, or network locations.

```csharp
// This is passed to the handler
public record BeforeAndAfterFileLocationRequest(
    IEnumerable<string> ProvidedBeforeTrackingFiles)
    : IRequest<BeforeAndAfterFileLocationResponse>;

// You will return this from your handler's Handle method
public record BeforeAndAfterFileLocationResponse(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths);
```

**Use cases:**
- **Cloud Storage**: Read from Azure Blob, AWS S3, or Google Cloud Storage
- **Database Integration**: Query tracking data from SQL databases
- **Network Shares**: Access data from network drives or shared storage

### ReadInBeforeAndAfterDataRequest

{% code-callout title="Data Processing" %}
**Default handler implemented** - Convert file locations into `TestData` objects, with support for custom data processing and aggregation.
{% /code-callout %}

Used to convert file locations into `TestData` objects that can be passed to the analyzer functions. May be used to bypass file downloads when reading data from cloud storage.

```csharp
public record ReadInBeforeAndAfterDataRequest(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths)
    : IRequest<ReadInBeforeAndAfterDataResponse>;

public record ReadInBeforeAndAfterDataResponse(
    TestData? BeforeData,
    TestData? AfterData);
```

**Use cases:**
- **Data Aggregation**: Combine multiple data sources before analysis
- **Format Conversion**: Transform data from custom formats
- **Preprocessing**: Clean or filter data before statistical analysis

## 📊 Test Lifecycle Notifications

{% info-callout title="Event-Driven Architecture" %}
Sailfish provides comprehensive notifications throughout the test lifecycle, enabling you to build event-driven integrations and monitoring systems.
{% /info-callout %}

### Key Lifecycle Events

{% feature-grid columns=2 %}
{% feature-card title="Test Case Events" description="TestCaseStartedNotification, TestCaseCompletedNotification, TestCaseDisabledNotification" /%}

{% feature-card title="Test Run Events" description="TestRunCompletedNotification, TestClassCompletedNotification" /%}

{% feature-card title="Analysis Events" description="SailDiffAnalysisCompleteNotification, ScaleFishAnalysisCompleteNotification" /%}

{% feature-card title="Error Handling" description="TestCaseExceptionNotification for robust error handling and logging" /%}
{% /feature-grid %}

### Example: Custom Test Monitoring

```csharp
public class CustomTestMonitoringHandler :
    INotificationHandler<TestCaseStartedNotification>,
    INotificationHandler<TestCaseCompletedNotification>,
    INotificationHandler<TestRunCompletedNotification>
{
    private readonly ILogger<CustomTestMonitoringHandler> logger;
    private readonly IMetricsCollector metricsCollector;

    public CustomTestMonitoringHandler(
        ILogger<CustomTestMonitoringHandler> logger,
        IMetricsCollector metricsCollector)
    {
        this.logger = logger;
        this.metricsCollector = metricsCollector;
    }

    public async Task Handle(TestCaseStartedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Test case started: {TestCase}", notification.TestInstanceContainer.DisplayName);
        await metricsCollector.IncrementCounter("sailfish.tests.started");
    }

    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Test case completed: {TestCase}", notification.TestInstanceContainerExternal.DisplayName);

        // Send metrics to monitoring system
        await metricsCollector.RecordGauge(
            "sailfish.test.duration",
            notification.ClassExecutionSummaryTrackingFormat.Mean);
    }

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Test run completed with {Count} test classes", notification.ClassExecutionSummaries.Count());

        // Generate custom report
        await GenerateCustomReport(notification.ClassExecutionSummaries);
    }
}
```

## 🚀 Advanced Integration Examples

### Cloud Storage Integration

{% code-callout title="Azure Blob Storage Example" %}
Integrate with Azure Blob Storage to store and retrieve performance tracking data.
{% /code-callout %}

```csharp
public class AzureBlobTrackingHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly BlobServiceClient blobServiceClient;

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient("performance-data");
        var blobName = $"tracking-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json";

        var trackingData = JsonSerializer.Serialize(notification.ClassExecutionSummaries);
        await containerClient.UploadBlobAsync(blobName, new BinaryData(trackingData));
    }
}
```

### Database Integration

{% success-callout title="Performance Data Warehouse" %}
Store performance results in a database for historical analysis and trending.
{% /success-callout %}

```csharp
public class DatabaseTrackingHandler : INotificationHandler<TestCaseCompletedNotification>
{
    private readonly IPerformanceDataRepository repository;

    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        var performanceRecord = new PerformanceRecord
        {
            TestName = notification.TestInstanceContainerExternal.DisplayName,
            Mean = notification.ClassExecutionSummaryTrackingFormat.Mean,
            Median = notification.ClassExecutionSummaryTrackingFormat.Median,
            StandardDeviation = notification.ClassExecutionSummaryTrackingFormat.StdDev,
            Timestamp = DateTime.UtcNow
        };

        await repository.SavePerformanceRecordAsync(performanceRecord);
    }
}
```

{% note-callout title="Getting Started" %}
Ready to extend Sailfish? Start by implementing a simple notification handler for logging, then gradually add more sophisticated integrations as your needs grow. Check out our [Example App](/docs/3/example-app) for a complete working example.
{% /note-callout %}
