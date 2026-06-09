using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation.Console;

namespace Sailfish.Presentation;

/// <summary>
///     Writes the run's execution summaries to the console. Previously the handler for the internal
///     <c>WriteToConsoleNotification</c>; collapsed to a direct service call (the notification had exactly
///     one framework-owned handler, so the mediator indirection bought nothing).
/// </summary>
internal interface IConsoleSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class ConsoleSummaryWriter : IConsoleSummaryWriter
{
    private readonly IConsoleWriter _consoleWriter;
    private readonly IRunSettings _runSettings;

    public ConsoleSummaryWriter(IConsoleWriter consoleWriter, IRunSettings runSettings)
    {
        _consoleWriter = consoleWriter;
        _runSettings = runSettings;
    }

    public Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        _consoleWriter.WriteToConsole(executionSummaries, _runSettings.Tags);
        return Task.CompletedTask;
    }
}
