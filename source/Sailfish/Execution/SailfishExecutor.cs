using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal class SailfishExecutor(
    IMediator mediator,
    ISailFishTestExecutor sailFishTestExecutor,
    ITestCollector testCollector,
    ITestFilter testFilter,
    IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
    IExecutionSummaryWriter executionSummaryWriter,
    ISailDiffInternal sailDiff,
    IScaleFishInternal scaleFish,
    IRunSettings runSettings,
    ILogger logger
    )
{
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler = classExecutionSummaryCompiler;
    private readonly IExecutionSummaryWriter executionSummaryWriter = executionSummaryWriter;
    private readonly ILogger logger = logger;
    private readonly IMediator mediator = mediator;
    private readonly IRunSettings runSettings = runSettings;
    private readonly ISailDiffInternal sailDiff = sailDiff;
    private readonly ISailFishTestExecutor sailFishTestExecutor = sailFishTestExecutor;
    private readonly IScaleFishInternal scaleFish = scaleFish;
    private readonly ITestCollector testCollector = testCollector;
    private readonly ITestFilter testFilter = testFilter;

    public async Task<SailfishRunResult> Run(CancellationToken cancellationToken)
    {
        var testInitializationResult = CollectTests(runSettings.TestNames, runSettings.TestLocationAnchors.ToArray());
        if (testInitializationResult.IsValid)
        {
            var testClassResultGroups = await sailFishTestExecutor.Execute(testInitializationResult.Tests, cancellationToken).ConfigureAwait(false);
            var classExecutionSummaries = classExecutionSummaryCompiler.CompileToSummaries(testClassResultGroups).ToList();

            await executionSummaryWriter.Write(classExecutionSummaries, cancellationToken);
            await mediator.Publish(new TestRunCompletedNotification(classExecutionSummaries.ToTrackingFormat()), cancellationToken).ConfigureAwait(false);

            if (runSettings.RunSailDiff) await sailDiff.Analyze(cancellationToken).ConfigureAwait(false);

            if (runSettings.RunScaleFish) await scaleFish.Analyze(cancellationToken).ConfigureAwait(false);

            var exceptions = classExecutionSummaries
                .SelectMany(classExecutionSummary =>
                    classExecutionSummary
                        .CompiledTestCaseResults
                        .Where(e => e.Exception is not null)
                        .Select(c => c.Exception))
                .Cast<Exception>()
                .ToList();

            return SailfishRunResult.CreateResult(classExecutionSummaries, exceptions);
        }

        logger.Log(LogLevel.Error, "{NumErrors} errors encountered while discovering tests",
            testInitializationResult.Errors.Count);

        var testDiscoveryExceptions = new List<Exception>();
        foreach ((var reason, var names) in testInitializationResult.Errors)
        {
            logger.Log(LogLevel.Error, "{Reason}", reason);
            foreach (var testName in names)
            {
                logger.Log(LogLevel.Error, "--- {TestName}", testName);
                testDiscoveryExceptions.Add(new Exception($"Test: {testName} - Error: {reason}"));
            }
        }

        return SailfishRunResult.CreateResult(Array.Empty<IClassExecutionSummary>(), testDiscoveryExceptions);
    }

    private TestInitializationResult CollectTests(IEnumerable<string> testNames, IEnumerable<Type> locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}