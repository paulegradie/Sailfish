using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation;
using Sailfish.Presentation.Csv;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteToMarkdownHandler : INotificationHandler<WriteToMarkDownCommand>
{
    private readonly IPerformanceCsvWriter performanceCsvWriter;

    public SailfishWriteToMarkdownHandler(IPerformanceCsvWriter performanceCsvWriter)
    {
        this.performanceCsvWriter = performanceCsvWriter;
    }

    public async Task Handle(WriteToMarkDownCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.DefaultPerformanceFileNameStem(notification.TimeStamp) + ".md";
        await performanceCsvWriter.Present(notification.Content, fileName);
    }
}