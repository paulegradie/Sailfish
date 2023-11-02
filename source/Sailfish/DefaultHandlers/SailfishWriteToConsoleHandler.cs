using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers;

internal class WriteToConsoleHandler : INotificationHandler<WriteToConsoleNotification>
{
    private readonly IConsoleWriter consoleWriter;
    private readonly IRunSettings runSettings;

    public WriteToConsoleHandler(IConsoleWriter consoleWriter, IRunSettings runSettings)
    {
        this.consoleWriter = consoleWriter;
        this.runSettings = runSettings;
    }

    public Task Handle(WriteToConsoleNotification notification, CancellationToken cancellationToken)
    {
        consoleWriter.Present(notification.Content, runSettings.Tags);
        return Task.CompletedTask;
    }
}