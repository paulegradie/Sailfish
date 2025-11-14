using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    Task Run(List<TestCase> testCases, CancellationToken cancellationToken);
}

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly ISailfishConsoleWindowFormatter _sailfishConsoleWindowFormatter;
    private readonly ITestAdapterExecutionEngine _testAdapterExecutionEngine;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IMediator mediator,
        ILogger logger,
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ITestCaseCountPrinter testCaseCountPrinter)
    {
        _testAdapterExecutionEngine = testAdapterExecutionEngine;
        _mediator = mediator;
        _logger = logger;
        _sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        _testCaseCountPrinter = testCaseCountPrinter;
    }

    public async Task Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            _logger.Log(LogLevel.Information, "No Sailfish tests were discovered");
            return;
        }

        _testCaseCountPrinter.SetTestCaseTotal(testCases.Count);
        _testCaseCountPrinter.PrintDiscoveredTotal();

        var executionSummaries = await _testAdapterExecutionEngine.Execute(testCases, cancellationToken);
        await _mediator
            .Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken)
            .ConfigureAwait(false);
    }
}