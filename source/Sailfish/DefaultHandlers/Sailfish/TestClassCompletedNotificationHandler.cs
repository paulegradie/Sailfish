using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.Sailfish;

public class TestClassCompletedNotificationHandler : INotificationHandler<TestClassCompletedNotification>
{
    private readonly ILogger _logger;
    private readonly IRunSettings _runSettings;
    private readonly ITrackingFileSerialization _trackingFileSerialization;

    public TestClassCompletedNotificationHandler(ITrackingFileSerialization trackingFileSerialization, IRunSettings runSettings, ILogger logger)
    {
        this._logger = logger;
        this._runSettings = runSettings;
        this._trackingFileSerialization = trackingFileSerialization;
    }

    public async Task Handle(TestClassCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (_runSettings.StreamTrackingUpdates is false) return;
        if (_runSettings.CreateTrackingFiles is false) return;

        var output = _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(output)) Directory.CreateDirectory(output);

        var trackingDirectory = _runSettings.GetRunSettingsTrackingDirectoryPath();
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTrackingFileName(_runSettings.TimeStamp), _runSettings.Tags);
        var filePath = Path.Join(trackingDirectory, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream);
        var fileContents = await streamReader.ReadToEndAsync(); // ct overload not available in .net6
        streamReader.Close();

        var classExecutionSummaryTrackingFormats = string.IsNullOrEmpty(fileContents)
            ? []
            : _trackingFileSerialization.Deserialize(fileContents)?.ToList() ?? [];

        foreach (var failedSummary in notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases())
            _logger.Log(LogLevel.Warning, failedSummary.Exception!, "Test case exception encountered");

        var success = notification.ClassExecutionSummaryTrackingFormat.FilterForSuccessfulTestCases();
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

        var serialized = _trackingFileSerialization.Serialize(classExecutionSummaryTrackingFormats);

        await using var streamWriter = new StreamWriter(filePath, Encoding.UTF8, new FileStreamOptions
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate
        });
        await streamWriter.WriteAsync(serialized).ConfigureAwait(false);
    }
}