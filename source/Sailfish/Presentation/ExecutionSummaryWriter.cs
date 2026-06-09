using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal interface IExecutionSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class ExecutionSummaryWriter : IExecutionSummaryWriter
{
    private readonly IConsoleSummaryWriter _consoleSummaryWriter;
    private readonly ICsvSummaryWriter _csvSummaryWriter;
    private readonly IMarkdownSummaryWriter _markdownSummaryWriter;

    public ExecutionSummaryWriter(
        IConsoleSummaryWriter consoleSummaryWriter,
        IMarkdownSummaryWriter markdownSummaryWriter,
        ICsvSummaryWriter csvSummaryWriter)
    {
        _consoleSummaryWriter = consoleSummaryWriter;
        _markdownSummaryWriter = markdownSummaryWriter;
        _csvSummaryWriter = csvSummaryWriter;
    }

    public async Task Write(
        List<IClassExecutionSummary> executionSummaries,
        CancellationToken cancellationToken)
    {
        // Direct, ordered writes — no mediator indirection. Console first (so results print before the file
        // writes begin), then markdown, then CSV. These were three internal "command" notifications
        // (WriteTo{Console,MarkDown,Csv}Notification) each dispatched to a single framework-owned handler;
        // the broadcast bus bought nothing. A thrown write now propagates to SailfishExecutor's
        // post-measurement boundary, which records it on the run result (fail-loud).
        await _consoleSummaryWriter.Write(executionSummaries, cancellationToken).ConfigureAwait(false);
        await _markdownSummaryWriter.Write(executionSummaries, cancellationToken).ConfigureAwait(false);
        await _csvSummaryWriter.Write(executionSummaries, cancellationToken).ConfigureAwait(false);
    }
}
