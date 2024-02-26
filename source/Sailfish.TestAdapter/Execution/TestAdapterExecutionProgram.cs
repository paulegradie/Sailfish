using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Execution;

public interface ITestAdapterExecutionProgram
{
    Task Run(List<TestCase> testCases, CancellationToken cancellationToken);
}

internal class TestAdapterExecutionProgram : ITestAdapterExecutionProgram
{
    private readonly IMediator mediator;
    private readonly ILogger logger;
    private readonly ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter;
    private readonly ITestCaseCountPrinter testCaseCountPrinter;
    private readonly ITestAdapterExecutionEngine testAdapterExecutionEngine;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IMediator mediator,
        ILogger logger,
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ITestCaseCountPrinter testCaseCountPrinter)
    {
        this.testAdapterExecutionEngine = testAdapterExecutionEngine;
        this.mediator = mediator;
        this.logger = logger;
        this.sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        this.testCaseCountPrinter = testCaseCountPrinter;
    }

    public async Task Run(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count == 0)
        {
            logger.Log(LogLevel.Information, "No Sailfish tests were discovered");
            return;
        }

        testCaseCountPrinter.SetTestCaseTotal(testCases.Count);
        testCaseCountPrinter.PrintDiscoveredTotal();

        var executionSummaries = await testAdapterExecutionEngine.Execute(testCases, cancellationToken);
        await mediator
            .Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken)
            .ConfigureAwait(false);
    }
}