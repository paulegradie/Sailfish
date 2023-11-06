---
title: Extensibility Commands
---
## Introduction

Sailfish exposes several public MediatR commands. Implement MediatR handlers for these commands to furhter customize Sailfish behavior.

## BeforeAndAfterFileLocationRequest

- **Default handler implemented**
- Used to provide tracking file location data to the t-test executor. E.g. Reading tracking data from blob storage.
- Registering an implementation of this will customize existing behaviour

```csharp

// This is passed to the handler
public class BeforeAndAfterFileLocationRequest : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationRequest(IEnumerable<string> providedBeforeTrackingFiles)
    {
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
    }

    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
}

// you will return this from your handler's Handle method
public class BeforeAndAfterFileLocationResponse
{
    public BeforeAndAfterFileLocationResponse(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
    }

    public IEnumerable<string> BeforeFilePaths { get; set; }
    public IEnumerable<string> AfterFilePaths { get; set; }
}
```

---

## ReadInBeforeAndAfterDataRequest

- **Default handler implemented**
- Used to convert file locations into `TestData` objects that can be passed to the analyzer functions
- May be used to bypass the need to download data when read data from cloud storage
- Registering an implementation of this will customize existing behaviour

```csharp
public class ReadInBeforeAndAfterDataRequest : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePaths { get; }
    public IEnumerable<string> AfterFilePaths { get; }

    public ReadInBeforeAndAfterDataRequest(IEnumerable<string> beforeFilePaths, IEnumerable<string> afterFilePaths)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
    }
}
```
---



## GetLatestExecutionSummaryRequest

- Used to specify the latest execution summary for both Saildiff and ScaleFish

```csharp
public class GetLatestExecutionSummaryRequest : IRequest<GetLatestExecutionSummaryResponse>
{
}
```
---

## TestCaseCompletedNotification

 - Invoked after completion of a single test case
 - Used to stream individual test cases for tracking or otherwise


 ```csharp
 public class TestCaseCompletedNotification : INotification
{
    public TestCaseCompletedNotification(ClassExecutionSummaryTrackingFormat testCaseExecutionResult)
    {
        TestCaseExecutionResult = testCaseExecutionResult;
    }

    public ClassExecutionSummaryTrackingFormat TestCaseExecutionResult { get; }
}
 ```

---

## TestRunCompletedNotification

- Invoked after completion of the full test run
- Used to write final tracking data

```csharp
public class TestRunCompletedNotification : INotification
{
    public TestRunCompletedNotification(IEnumerable<ClassExecutionSummaryTrackingFormat> classExecutionSummaries)
    {
        ClassExecutionSummaries = classExecutionSummaries;
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat> ClassExecutionSummaries { get; }
}
```

---



## ScalefishAnalysisCompleteNotification

- Invoked on completion of Scalefish analysis
- Used to write model selection and model fitting result

```csharp
public class ScalefishAnalysisCompleteNotification : INotification
{
    public ScalefishAnalysisCompleteNotification(string scalefishResultMarkdown, List<IScalefishClassModels> testClassComplexityResults)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        TestClassComplexityResults = testClassComplexityResults;
    }

    public List<IScalefishClassModels> TestClassComplexityResults { get; }
    public string ScalefishResultMarkdown { get; }
}
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
