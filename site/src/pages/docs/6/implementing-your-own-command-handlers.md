# Implementing your own command handlers for custom behavior.

Commands are packets of information that get passed to a command handler. The handler is a type you will implement in order to override default handlers and their functionality to introduce your own logic.

For example, if we wanted to implement the `BeforeAndAfterFileLocationCommand`, your implementation might look something like this (customized of course as you need):

```csharp
public class CustomHandler: IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>
{
    private readonly ITrackingFileFinder trackingFileFinder;

    public CustomHandler(ITrackingFileFinder trackingFileFinder)
    {
        this.trackingFileFinder = trackingFileFinder;
    }

    public async Task<BeforeAndAfterFileLocationResponse> Handle(BeforeAndAfterFileLocationCommand request, CancellationToken cancellationToken)
    {
        var trackingFiles = trackingFileFinder.GetBeforeAndAfterTrackingFiles(request.DefaultDirectory, request.BeforeTarget, request.Tags);
        await Task.CompletedTask;

        return new BeforeAndAfterFileLocationResponse(trackingFiles.BeforeFilePath, trackingFiles.AfterFilePath);
    }
}
```

You would then register this implementation as described in the [Registering Dependencies for your tests](../4/registering-dependencies-for-your-tests.md) section.

For example:
```csharp
builder.RegisterType<WriteTrackingDataToAzureBlobHandler>().As<INotificationHandler<WriteCurrentTrackingFileCommand>>();

builder.RegisterType<ReadBeforeAndAfterFromBlobStorageHandler>().As<IRequestHandler<BeforeAndAfterFileLocationCommand, BeforeAndAfterFileLocationResponse>>();
```

## Next: [Statistical Analysis](../7/statistical-analysis.md)
