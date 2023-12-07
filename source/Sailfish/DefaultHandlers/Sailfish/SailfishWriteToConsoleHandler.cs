using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation.Console;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class WriteToConsoleHandler(IConsoleWriter consoleWriter, IRunSettings runSettings) : INotificationHandler<WriteToConsoleNotification>
{
    private readonly IConsoleWriter consoleWriter = consoleWriter;
    private readonly IRunSettings runSettings = runSettings;

    public Task Handle(WriteToConsoleNotification notification, CancellationToken cancellationToken)
    {
        consoleWriter.WriteToConsole(notification.Content, runSettings.Tags);
        return Task.CompletedTask;
    }
}