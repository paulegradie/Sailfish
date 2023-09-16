---
title: SailDiff
---

**SailDiff** is a tool for running automated statistical testing on sailfish tracking data.

Add a .sailfish.json files to the test project to enable saildiff when running via the test adapter.

When enabled, tracking data will be used for comparison to the current run and will produce a Saildiff outputs result file showing the before and after data, as well as a measure on whether or not the data is significantly different.


## Customizing the SailDiff inputs

By default, Sailfish will look for the most recent file in the default tracking directory when you execute a test run via a console app.

The flow of the analysis is

1. Program Execution
1. WriteDataHandler
1. `IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>`
1. `IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>`
1. Saildiff / Scalefish

This flow shows that there are two points at which you can minipulate the data inputs:

- IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
- IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>


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

SailDiff will automatically aggregate data when multiple files are provided.