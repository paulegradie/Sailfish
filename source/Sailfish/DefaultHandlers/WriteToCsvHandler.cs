using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation;
using Sailfish.Presentation.CsvAndJson;

namespace Sailfish.DefaultHandlers;

internal class WriteToCsvHandler : INotificationHandler<WriteToCsvCommand>
{
    private readonly IPerformanceResultPresenter performanceResultPresenter;

    public WriteToCsvHandler(IPerformanceResultPresenter performanceResultPresenter)
    {
        this.performanceResultPresenter = performanceResultPresenter;
    }

    public async Task Handle(WriteToCsvCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(notification.TimeStamp) + ".csv", notification.Tags);
        var filePath = Path.Combine(notification.OutputDirectory, fileName);
        await performanceResultPresenter.WriteToFileAsCsv(notification.Content, filePath, summary => summary.Settings.AsCsv, cancellationToken).ConfigureAwait(false);
    }
}