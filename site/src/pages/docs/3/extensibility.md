---
title: Extensibility Commands
---
## Introduction

Sailfish exposes several public MediatR commands. Implement MediatR handlers for these commands to furhter customize Sailfish behavior.

## BeforeAndAfterFileLocationCommand

- **Default handler implemented**
- Used to provide tracking file location data to the t-test executor. E.g. Reading tracking data from blob storage.
- Registering an implementation of this will customize existing behaviour

```csharp

// This is passed to the handler
public class BeforeAndAfterFileLocationCommand : IRequest<BeforeAndAfterFileLocationResponse>
{
    public BeforeAndAfterFileLocationCommand(string defaultDirectory, OrderedDictionary<string, string> tags, string beforeTarget, OrderedDictionary<string, string> args)
    {
        DefaultDirectory = defaultDirectory;
        Tags = tags;
        BeforeTarget = beforeTarget;
        Args = args;
    }

    public string DefaultDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public string BeforeTarget { get; }
    public OrderedDictionary<string, string> Args { get; }
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

## ReadInBeforeAndAfterDataCommand

- **Default handler implemented**
- Used to convert file locations into `TestData` objects that can be passed to the analyzer functions
- May be used to bypass the need to download data when read data from cloud storage
- Registering an implementation of this will customize existing behaviour

```csharp
public class ReadInBeforeAndAfterDataCommand : IRequest<ReadInBeforeAndAfterDataResponse>
{
    public IEnumerable<string> BeforeFilePath { get; set; }
    public IEnumerable<string> AfterFilePath { get; set; }
    public OrderedDictionary<string, string> Tags { get; set; }
    public OrderedDictionary<string, string> Args { get; set; }
    public string BeforeTarget { get; set; }

    public ReadInBeforeAndAfterDataCommand(
        IEnumerable<string> beforeFilePath,
        IEnumerable<string> afterFilePath,
        string beforeTarget,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
        BeforeTarget = beforeTarget;
        Tags = tags;
        Args = args;
    }
}
```

---

## NotifyOnTestResultCommand

- Used to induce behaviour when a t-test result is produced. E.g. writing the t-test result to a blob storage container for later consumption.

```csharp
public class NotifyOnTestResultCommand : INotification
{
    public NotifyOnTestResultCommand(
        TestResultFormats testResultFormats,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        TestResultFormats = testResultFormats;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public TestResultFormats TestResultFormats { get; }
    public TestSettings TestSettings { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
}
```

---
## SailfishGetLatestExecutionSummaryCommand

- Used to specify the latest execution summary for both Saildiff and ScaleFish

```csharp
public class SailfishGetLatestExecutionSummaryCommand : IRequest<SailfishGetLatestExecutionSummaryResponse>
{
    public SailfishGetLatestExecutionSummaryCommand(
        string trackingDirectory,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TrackingDirectory = trackingDirectory;
        Tags = tags;
        Args = args;
    }

    public string TrackingDirectory { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
}
```
---

## WriteCurrentScalefishResultCommand

- Used to specify where to write ScaleFish markdown results to

```csharp
public class WriteCurrentScalefishResultCommand : INotification
{
    public WriteCurrentScalefishResultCommand(
        string scalefishResultMarkdown,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        ScalefishResultMarkdown = scalefishResultMarkdown;
        LocalOutputDirectory = localOutputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultScalefishFileName(timeStamp);
    }

    public string ScalefishResultMarkdown { get; }
    public string LocalOutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}
```

---
## WriteCurrentScalefishResultModelsCommand

- Used to specify where and how to write ScaleFish model results. This is a `json` file.

```csharp
public class WriteCurrentScalefishResultModelsCommand : INotification
{
    public WriteCurrentScalefishResultModelsCommand(
        List<IScalefishClassModels> testClassComplexityResults,
        string localOutputDirectory,
        DateTime timeStamp,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TestClassComplexityResults = testClassComplexityResults;
        LocalOutputDirectory = localOutputDirectory;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultScalefishModelFileName(timeStamp);
    }

    public List<IScalefishClassModels> TestClassComplexityResults { get; }
    public string LocalOutputDirectory { get; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public string DefaultFileName { get; }
}
```


---
## WriteCurrentTrackingFileCommand

- **Default handler implemented**
- Used to direct tracking file outputs to a custom location. E.g. Writing tracking files to blob storage.
- Registering an implementation of this will customize existing behavior

```csharp
public class WriteCurrentTrackingFileCommand : INotification
{
    public WriteCurrentTrackingFileCommand(string trackingFileTrackingFileContent, string defaultOutputDirectory, DateTime timeStamp, OrderedDictionary<string, string> tags, OrderedDictionary<string, string> args)
    {
        TrackingFileContent = trackingFileTrackingFileContent;
        DefaultOutputDirectory = defaultOutputDirectory;
        Tags = tags;
        Args = args;
        DefaultFileName = DefaultFileSettings.DefaultTrackingFileName(timeStamp);
    }

    public string TrackingFileContent { get; set; }
    public string DefaultOutputDirectory { get; set; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public string DefaultFileName { get; }
}
```

---

## WriteTestResultsAsMarkdownCommand

- Used to direct t-test markdown result files to a custom location. E.g. Writing to blob storage.

```csharp
public class WriteTestResultsAsMarkdownCommand : INotification
{
    public WriteTestResultsAsMarkdownCommand(
        string markdownTable,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        MarkdownTable = markdownTable;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        TimeStamp = timeStamp;
        Tags = tags;
        Args = args;
    }

    public string MarkdownTable { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime TimeStamp { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }
    public TestSettings TestSettings { get; set; }
}
```

---

## WriteTestResultsAsCsvCommand

- Used to direct the t-test csv result file to a custom location. E.g. Blob storage.

```csharp
public class WriteTestResultsAsCsvCommand : INotification
{
    public readonly DateTime TimeStamp;
    public List<TestCaseResults> CsvFormat { get; }
    public string OutputDirectory { get; }
    public TestSettings TestSettings { get; }
    public OrderedDictionary<string, string> Tags { get; }
    public OrderedDictionary<string, string> Args { get; }

    public WriteTestResultsAsCsvCommand(
        List<TestCaseResults> csvFormat,
        string outputDirectory,
        TestSettings testSettings,
        DateTime timeStamp,
        OrderedDictionary<string, string> tags,
        OrderedDictionary<string, string> args)
    {
        TimeStamp = timeStamp;
        CsvFormat = csvFormat;
        OutputDirectory = outputDirectory;
        TestSettings = testSettings;
        Tags = tags;
        Args = args;
    }
}
```
