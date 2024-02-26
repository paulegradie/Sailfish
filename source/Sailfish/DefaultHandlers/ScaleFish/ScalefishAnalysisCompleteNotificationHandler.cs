using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Presentation;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.DefaultHandlers.ScaleFish;

internal class ScaleFishAnalysisCompleteNotificationHandler : INotificationHandler<ScaleFishAnalysisCompleteNotification>
{
    private readonly IRunSettings runSettings;

    public ScaleFishAnalysisCompleteNotificationHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task Handle(ScaleFishAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(runSettings.LocalOutputDirectory)) Directory.CreateDirectory(runSettings.LocalOutputDirectory);

        await WriteResults(DefaultFileSettings.DefaultScalefishFileName(runSettings.TimeStamp), notification.ScaleFishResultMarkdown, cancellationToken);
        await WriteModels(DefaultFileSettings.DefaultScalefishModelFileName(runSettings.TimeStamp), notification.TestClassComplexityResults, cancellationToken);
    }

    private async Task WriteModels(string fileName, List<ScalefishClassModel> models, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, runSettings.Tags));
        var result = SailfishSerializer.Serialize(models);
        await WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteResults(string fileName, string result, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, runSettings.Tags));
        await WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteStringToFile(string contents, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

        await File.WriteAllTextAsync(filePath, contents, cancellationToken).ConfigureAwait(false);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
    }
}