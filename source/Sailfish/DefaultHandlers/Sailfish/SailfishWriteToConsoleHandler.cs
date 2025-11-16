using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class WriteToConsoleHandler : INotificationHandler<WriteToConsoleNotification>
{
    private readonly IConsoleWriter _consoleWriter;
    private readonly IRunSettings _runSettings;

    public WriteToConsoleHandler(IConsoleWriter consoleWriter, IRunSettings runSettings)
    {
        _consoleWriter = consoleWriter;
        _runSettings = runSettings;
    }

    public Task Handle(WriteToConsoleNotification notification, CancellationToken cancellationToken)
    {
        _consoleWriter.WriteToConsole(notification.Content, _runSettings.Tags);
        return Task.CompletedTask;
    }
}