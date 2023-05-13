---
title: Customizing Sailfish Analysis
---

By default, Sailfish will look for the most recent file in the default tracking directory when you execute a test run via a console app.

> **Note**: Setting tracking directories via the test adapter is not yet supported. This is a feature that will be released in the future.

If you'd like to customize the behavior of Sailfish's analysis logic, you can use various extensibility points. These points expose ways to modify various behaviors of the system, so here, we'll focus on those that relate to analysis features.

## Customizing the analysis inputs

The flow of the analysis is

Program Execution > WriteDataHandler > `IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>` > `IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>` > Analyze

This flow shows that there are two points at which you can minipulate the data inputs:

- IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
- IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>

## Default Handlers

To help you understand how to implement your custom handlers, lets have a quick look at the default handlers.

> Remember to take some time to familiarize yourself with the various `Commands` that are passed to these handlers

### Reading Tracking Data from a custom location

```csharp
internal class SailfishBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileFinder trackingFileFinder;

    public SailfishBeforeAndAfterFileLocationHandler(ITrackingFileFinder trackingFileFinder)
    {
        this.trackingFileFinder = trackingFileFinder;
    }

    public Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.DefaultDirectory, request.BeforeTarget, request.Tags);
        // Consider reading data from a:
        // - cloud storage container
        // - cloud log processing tool
        // - network drive
        // - local directory
        return Task.FromResult(new BeforeAndAfterFileLocationResponse(
            new List<string>() { trackingFiles.BeforeFilePath }.Where(x => !string.IsNullOrEmpty(x)),
            new List<string>() { trackingFiles.AfterFilePath }.Where(x => !string.IsNullOrEmpty(x))));
    }
}
```

### Reading Tracking Data that you wish to aggregate prior to testing

`
This handler is available for scenarios where simply pointing to the right file location is not sufficient, and instead need to manaully combine your data in a particular way. For example, you need to combine the last N files based on an external configuration for the 'before' data set, and one or more files worth of data for the 'after' data set. by hooking into this step, you can perform custom outlier removal, heuristic-based processing, or other pre-processing as you wish.

```csharp
internal class SailfishReadInBeforeAndAfterDataHandler : IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>
{
    public async Task<ReadInBeforeAndAfterDataResponse> Handle(ReadInBeforeAndAfterDataCommand request, CancellationToken cancellationToken)
    {

        // When you return the data, you are also required to provide an IEnumerable<string> that represents the files that were used.
        return new ReadInBeforeAndAfterDataResponse(new TestData(dataSourcesBefore, beforeData), new TestData(dataSourcesAfter, afterData));
    }
}
```

If you inspect the `TestData` source code, you will find that it takes an IEnumerable of test Ids, which are intended for you to keep track of which processed files were used in the statistical test.

## Aggretating data for your analysis

The default behavior of the Analyzer is to read a single before and after file, however Sailfish is actually capable of automatically aggregating as many result files as you'd like. The only requirement is that the data satisifies the correct struture when being returned from the handler.

We'll explore more about extensibility points in the next section.
