using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.Execution;

internal class SailfishExecutor
{
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IExecutionSummaryWriter executionSummaryWriter;
    private readonly IRunSettings runSettings;
    private readonly IMediator mediator;
    private readonly ISailFishTestExecutor sailFishTestExecutor;
    private readonly ISailDiffInternal sailDiff;
    private readonly IScaleFishInternal scaleFish;

    public SailfishExecutor(
        IMediator mediator,
        ISailFishTestExecutor sailFishTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IExecutionSummaryWriter executionSummaryWriter,
        ISailDiffInternal sailDiff,
        IScaleFishInternal scaleFish,
        IRunSettings runSettings
    )
    {
        this.mediator = mediator;
        this.sailFishTestExecutor = sailFishTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.executionSummaryWriter = executionSummaryWriter;
        this.sailDiff = sailDiff;
        this.scaleFish = scaleFish;
        this.runSettings = runSettings;
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

            if (runSettings.RunSailDiff)
            {
                await sailDiff.Analyze(cancellationToken).ConfigureAwait(false);
            }

            if (runSettings.RunScalefish)
            {
                await scaleFish.Analyze(cancellationToken).ConfigureAwait(false);
            }

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

        Log.Logger.Error("{NumErrors} errors encountered while discovering tests",
            testInitializationResult.Errors.Count);

        var testDiscoveryExceptions = new List<Exception>();
        foreach (var (reason, names) in testInitializationResult.Errors)
        {
            Log.Logger.Error("{Reason}", reason);
            foreach (var testName in names)
            {
                Log.Logger.Error("--- {TestName}", testName);
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