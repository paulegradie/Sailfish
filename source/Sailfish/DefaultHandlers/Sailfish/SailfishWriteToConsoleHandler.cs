using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class WriteToConsoleHandler(IConsoleWriter consoleWriter, IRunSettings runSettings) : INotificationHandler<WriteToConsoleNotification>
{
    private readonly IConsoleWriter _consoleWriter = consoleWriter;
    private readonly IRunSettings _runSettings = runSettings;

    public Task Handle(WriteToConsoleNotification notification, CancellationToken cancellationToken)
    {
        _consoleWriter.WriteToConsole(notification.Content, _runSettings.Tags);
        return Task.CompletedTask;
    }
}