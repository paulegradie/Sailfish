using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers.Sailfish;

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
        consoleWriter.WriteToConsole(notification.Content, runSettings.Tags);
        return Task.CompletedTask;
    }
}