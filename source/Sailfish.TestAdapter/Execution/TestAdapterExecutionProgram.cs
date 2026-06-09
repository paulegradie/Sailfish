using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
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
    private readonly IPublisher _publisher;
    private readonly ISailfishConsoleWindowFormatter _sailfishConsoleWindowFormatter;
    private readonly ITestAdapterExecutionEngine _testAdapterExecutionEngine;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;

    public TestAdapterExecutionProgram(
        IRunSettings runSettings,
        ITestAdapterExecutionEngine testAdapterExecutionEngine,
        IPublisher publisher,
        ILogger logger,
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ITestCaseCountPrinter testCaseCountPrinter)
    {
        _testAdapterExecutionEngine = testAdapterExecutionEngine;
        _publisher = publisher;
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
        await _publisher
            .Publish(new TestRunCompletedNotification(executionSummaries.ToTrackingFormat()), cancellationToken)
            .ConfigureAwait(false);
    }
}