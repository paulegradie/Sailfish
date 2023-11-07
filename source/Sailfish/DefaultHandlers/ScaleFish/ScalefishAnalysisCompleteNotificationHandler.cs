using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.ScaleFish;

internal class ScalefishAnalysisCompleteNotificationHandler : INotificationHandler<ScalefishAnalysisCompleteNotification>
{
    private readonly IFileIo fileIo;
    private readonly IRunSettings runSettings;

    public ScalefishAnalysisCompleteNotificationHandler(IFileIo fileIo, IRunSettings runSettings)
    {
        this.fileIo = fileIo;
        this.runSettings = runSettings;
    }

    public async Task Handle(ScalefishAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(runSettings.LocalOutputDirectory))
        {
            Directory.CreateDirectory(runSettings.LocalOutputDirectory);
        }

        await WriteResults(DefaultFileSettings.DefaultScalefishFileName(runSettings.TimeStamp), notification.ScalefishResultMarkdown, cancellationToken);
        await WriteModels(DefaultFileSettings.DefaultScalefishModelFileName(runSettings.TimeStamp), notification.TestClassComplexityResults, cancellationToken);
    }

    async Task WriteModels(string fileName, List<ScalefishClassModel> models, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, runSettings.Tags));
        var result = SailfishSerializer.Serialize(models);
        await fileIo.WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }

    async Task WriteResults(string fileName, string result, CancellationToken cancellationToken)
    {
        var filepath = Path.Join(runSettings.LocalOutputDirectory, DefaultFileSettings.AppendTagsToFilename(fileName, runSettings.Tags));
        await fileIo.WriteStringToFile(result, filepath, cancellationToken).ConfigureAwait(false);
    }
}