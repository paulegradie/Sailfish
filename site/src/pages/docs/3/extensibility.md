---
title: Test Lifecycle Event Hooks
---

## Introduction

Sailfish exposes a set of MediatR commands and notifications that fire during the test lifecycle and analysis pipeline.
By implementing handlers for these messages you can hook into Sailfish at well-defined points to customise behaviour:
for example loading tracking data from external storage, streaming results, or emitting metrics to your own observability stack.

## BeforeAndAfterFileLocationRequest

- **Default handler implemented**
- Used to provide tracking file location data to the statistical test executor. E.g. Reading tracking data from blob storage or somewher else.
- Registering an implementation of this will customize existing behaviour

```csharp

// This is passed to the handler
public record BeforeAndAfterFileLocationRequest(
    IEnumerable<string> ProvidedBeforeTrackingFiles)
    : IRequest<BeforeAndAfterFileLocationResponse>;

// you will return this from your handler's Handle method
public record BeforeAndAfterFileLocationResponse(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths);
```

---

## ReadInBeforeAndAfterDataRequest

- **Default handler implemented**
- Used to convert file locations into `TestData` objects that can be passed to the analyzer functions
- May be used to bypass the need to download data when read data from cloud storage
- Registering an implementation of this will customize existing behaviour

```csharp
public record ReadInBeforeAndAfterDataRequest(
    IEnumerable<string> BeforeFilePaths,
    IEnumerable<string> AfterFilePaths)
    : IRequest<ReadInBeforeAndAfterDataResponse>;


public record ReadInBeforeAndAfterDataResponse(
    TestData? BeforeData,
    TestData? AfterData)
```

---

## GetAllTrackingDataOrderedChronologicallyRequest

- **Default handler implemented**
- Returns a TrackingFileDataList in requested chronological order (ascending or descending)
- Overriding this will let you specify a custom list of ordered data
- This won't typically need to be overriden

```csharp
public class GetAllTrackingDataOrderedChronologicallyRequest(bool Ascending = false)
    : IRequest<GetAllTrackingDataOrderedChronologicallyResponse>;

public record GetAllTrackingDataOrderedChronologicallyResponse(TrackingFileDataList TrackingData);
```

---

## GetLatestExecutionSummaryRequest

- Used to specify the latest execution summary for both Saildiff and ScaleFish

```csharp
public record GetLatestExecutionSummaryRequest
    : IRequest<GetLatestExecutionSummaryResponse>;

public record GetLatestExecutionSummaryResponse(List<IClassExecutionSummary> LatestExecutionSummaries);
```

## TestCaseStartedNotification

- A notification that signals the start of a single test case

```csharp
public record TestCaseStartedNotification(
    TestInstanceContainerExternal TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup)
    : INotification;
```

---

## TestCaseCompletedNotification

- A notification that signals the completion of a single test case
- Used to stream individual test cases for tracking or otherwise

```csharp
public record TestCaseCompletedNotification(
    ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat,
    TestInstanceContainerExternal TestInstanceContainerExternal,
    IEnumerable<dynamic> TestCaseGroup
) : INotification;
```

---

## TestClassCompletedNotification

- A notification that signals the completion a single test class

```csharp
public record TestClassCompletedNotification(
    ClassExecutionSummaryTrackingFormat ClassExecutionSummaryTrackingFormat,
    TestInstanceContainerExternal TestInstanceContainerExternal,
    IEnumerable<dynamic> TestCaseGroup) : INotification;
```

---

## TestCaseDisabledNotification

- A notification that signals that the current test case is disabled

```csharp
internal record TestCaseDisabledNotification(
    TestInstanceContainerExternal TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup,
    bool DisableTheGroup)
    : INotification;
```

---

## TestCaseExceptionNotification

- A notification that signals that there was an exception in a test case

```csharp
public record TestCaseExceptionNotification(
    TestInstanceContainerExternal? TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup,
    Exception? Exception)
    : INotification;
```

---

## TestRunCompletedNotification

- A notification that signals the completion of the full test run
- Used to write final tracking data

```csharp
public record TestRunCompletedNotification(
    IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries)
    : INotification;
```

---

## ScaleFishAnalysisCompleteNotification

- Invoked on completion of Scalefish analysis
- Used to write model selection and model fitting result

```csharp
public record ScaleFishAnalysisCompleteNotification(
    string ScaleFishResultMarkdown,
    List<ScalefishClassModel> TestClassComplexityResults)
    : INotification;
```

---

## SailDiffAnalysisCompleteNotification

- Invoked on completion of a SailDiff analysis
- Used to write

```csharp
public class SailDiffAnalysisCompleteNotification : INotification
{
    public IEnumerable<TestCaseResults> TestCaseResults { get; }
    public string ResultsAsMarkdown { get; }

    public SailDiffAnalysisCompleteNotification(IEnumerable<TestCaseResults> testCaseResults, string resultsAsMarkdown)
    {
        TestCaseResults = testCaseResults;
        ResultsAsMarkdown = resultsAsMarkdown;
    }
}
```
