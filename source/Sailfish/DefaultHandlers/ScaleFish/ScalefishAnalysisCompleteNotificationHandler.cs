using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.ScaleFish;

internal class ScaleFishAnalysisCompleteNotificationHandler : INotificationHandler<ScaleFishAnalysisCompleteNotification>
{
    private readonly IRunSettings _runSettings;

    public ScaleFishAnalysisCompleteNotificationHandler(IRunSettings runSettings)
    {
        _runSettings = runSettings;
    }

    public async Task Handle(ScaleFishAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_runSettings.LocalOutputDirectory)) Directory.CreateDirectory(_runSettings.LocalOutputDirectory);

        await WriteResults(DefaultFileSettings.DefaultScalefishFileName(_runSettings.TimeStamp), notification.ScaleFishResultMarkdown, cancellationToken);
        await WriteModels(DefaultFileSettings.DefaultScalefishModelFileName(_runSettings.TimeStamp), notification.TestClassComplexityResults, cancellationToken);
    }

    private async Task WriteModels(string fileName, List<ScalefishClassModel> models, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(_runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, _runSettings.Tags));
        var result = SailfishSerializer.Serialize(models);
        await WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteResults(string fileName, string result, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(_runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, _runSettings.Tags));
        await WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteStringToFile(string contents, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to the provided directory");

        await File.WriteAllTextAsync(filePath, contents, cancellationToken).ConfigureAwait(false);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
    }
}