using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.Sailfish;

public class SailfishUpdateTrackingDataNotificationHandler : INotificationHandler<TestCaseCompletedNotification>
{
    private readonly ITrackingFileSerialization trackingFileSerialization;
    private readonly IRunSettings runSettings;
    private readonly ILogger logger;

    public SailfishUpdateTrackingDataNotificationHandler(ITrackingFileSerialization trackingFileSerialization, IRunSettings runSettings, ILogger logger)
    {
        this.trackingFileSerialization = trackingFileSerialization;
        this.runSettings = runSettings;
        this.logger = logger;
    }

    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (runSettings.StreamTrackingUpdates is false) return;
        if (runSettings.CreateTrackingFiles is false) return;

        var output = runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var trackingDirectory = runSettings.GetRunSettingsTrackingDirectoryPath();
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTrackingFileName(runSettings.TimeStamp), runSettings.Tags);
        var filePath = Path.Join(trackingDirectory, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream);
        var fileContents = await streamReader.ReadToEndAsync();
        streamReader.Close();

        var classExecutionSummaryTrackingFormats = string.IsNullOrEmpty(fileContents)
            ? new List<ClassExecutionSummaryTrackingFormat>()
            : trackingFileSerialization.Deserialize(fileContents)?.ToList() ?? new List<ClassExecutionSummaryTrackingFormat>();

        foreach (var failedSummary in notification.TestCaseExecutionResult.GetFailedTestCases())
        {
            logger.Log(LogLevel.Warning, failedSummary.Exception!, "Test case exception encountered");
        }

        var success = notification.TestCaseExecutionResult.FilterForSuccessfulTestCases();
        if (!success.GetSuccessfulTestCases().Any()) return;
        var preExistingSummary = classExecutionSummaryTrackingFormats.FirstOrDefault(x => x.TestClass.FullName == success.TestClass.FullName);
        if (preExistingSummary is not null)
        {
            var update = preExistingSummary.CompiledTestCaseResults.ToList();
            update.AddRange(success.CompiledTestCaseResults);
            preExistingSummary.CompiledTestCaseResults = update;
        }
        else
        {
            classExecutionSummaryTrackingFormats.Add(success);
        }

        var serialized = trackingFileSerialization.Serialize(classExecutionSummaryTrackingFormats);

        await using var streamWriter = new StreamWriter(filePath, Encoding.UTF8, new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate
        });
        await streamWriter.WriteAsync(serialized).ConfigureAwait(false);
    }
}