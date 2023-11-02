using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers;

internal class WriteToConsoleHandler : INotificationHandler<WriteToConsoleNotification>
{
    private readonly IConsoleWriter consoleWriter;

    public WriteToConsoleHandler(IConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public Task Handle(WriteToConsoleNotification notification, CancellationToken cancellationToken)
    {
        consoleWriter.Present(notification.Content, notification.Tags);
        return Task.CompletedTask;
    }
}