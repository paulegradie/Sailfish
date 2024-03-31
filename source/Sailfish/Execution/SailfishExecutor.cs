using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Exceptions;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.Execution;

internal class SailfishExecutor
{
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ISailDiffInternal sailDiff;
    private readonly ISailFishTestExecutor sailFishTestExecutor;
    private readonly IScaleFishInternal scaleFish;
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;

    public SailfishExecutor(IMediator mediator,
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IExecutionSummaryWriter executionSummaryWriter,
        ISailDiffInternal sailDiff,
        IScaleFishInternal scaleFish,
        IRunSettings runSettings,
        ILogger logger)
    {
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.executionSummaryWriter = executionSummaryWriter;
        this.logger = logger;
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.sailDiff = sailDiff;
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.scaleFish = scaleFish;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
    }

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
        foreach (var (reason, names) in testInitializationResult.Errors)
        {
            logger.Log(LogLevel.Error, "{Reason}", reason);
            foreach (var testName in names)
            {
                logger.Log(LogLevel.Error, "--- {TestName}", testName);
                testDiscoveryExceptions.Add(new SailfishException($"Test: {testName} - Error: {reason}"));
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