using System;
using System.IO;
using System.Linq;
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
        _logger = logger;
        _runSettings = runSettings;
        _trackingFileSerialization = trackingFileSerialization;
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

        // Read prior tracking contents WITHOUT creating the file. Opening the destination just to read it
        // (the previous behaviour) left a 0-byte artifact behind whenever serialization later threw or the
        // process was killed mid-write — and that empty file then tripped the next run's tracking-data
        // retrieval.
        var fileContents = File.Exists(filePath)
            ? await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false)
            : string.Empty;

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

        // Serialize BEFORE touching the destination so a serializer failure cannot leave a partial/empty
        // tracking file behind.
        var serialized = _trackingFileSerialization.Serialize(classExecutionSummaryTrackingFormats);

        await WriteAtomically(trackingDirectory, filePath, serialized, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Writes <paramref name="contents" /> to <paramref name="finalPath" /> atomically: the full payload
    ///     is staged to a temp file in the same directory and then renamed over the final name. A crash
    ///     mid-write leaves only the temp (cleaned up on failure, and never matched by tracking-file
    ///     discovery because it does not end in the <c>.json.tracking</c> suffix), never a partial
    ///     <c>*.json.tracking</c> that a later run would try to read.
    /// </summary>
    private static async Task WriteAtomically(string directory, string finalPath, string contents, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, contents, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch
            {
                // Best-effort cleanup; surface the original failure below.
            }

            throw;
        }
    }
}