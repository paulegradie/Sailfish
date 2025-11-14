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
    private readonly IClassExecutionSummaryCompiler _classExecutionSummaryCompiler;
    private readonly IExecutionSummaryWriter _executionSummaryWriter;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;
    private readonly ISailDiffInternal _sailDiff;
    private readonly ISailFishTestExecutor _sailFishTestExecutor;
    private readonly IScaleFishInternal _scaleFish;
    private readonly ITestCollector _testCollector;
    private readonly ITestFilter _testFilter;

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
        this._classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this._executionSummaryWriter = executionSummaryWriter;
        this._logger = logger;
        this._mediator = mediator;
        this._runSettings = runSettings;
        this._sailDiff = sailDiff;
        this._sailFishTestExecutor = sailFishTestExecutor;
        this._scaleFish = scaleFish;
        this._testCollector = testCollector;
        this._testFilter = testFilter;
    }

    public async Task<SailfishRunResult> Run(CancellationToken cancellationToken)
    {
        var testInitializationResult = CollectTests(_runSettings.TestNames, _runSettings.TestLocationAnchors.ToArray());
        if (testInitializationResult.IsValid)
        {
            // Optional seeded randomization of test class execution order for reproducibility
            var testsList = testInitializationResult.Tests.ToList();
            var seed = _runSettings.Seed ?? TryParseSeed(_runSettings.Args);
            if (seed.HasValue)
            {
                var rng = new Random(seed.Value);
                testsList = testsList.OrderBy(_ => rng.Next()).ToList();
                _logger.Log(LogLevel.Information, "Randomized test class execution order with seed {Seed}", seed.Value);
            }

            var testClassResultGroups = await _sailFishTestExecutor.Execute(testsList, cancellationToken).ConfigureAwait(false);
            var classExecutionSummaries = _classExecutionSummaryCompiler.CompileToSummaries(testClassResultGroups).ToList();

            await _executionSummaryWriter.Write(classExecutionSummaries, cancellationToken);
            await _mediator.Publish(new TestRunCompletedNotification(classExecutionSummaries.ToTrackingFormat()), cancellationToken).ConfigureAwait(false);

            if (_runSettings.RunSailDiff) await _sailDiff.Analyze(cancellationToken).ConfigureAwait(false);

            if (_runSettings.RunScaleFish) await _scaleFish.Analyze(cancellationToken).ConfigureAwait(false);

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

        _logger.Log(LogLevel.Error, "{NumErrors} errors encountered while discovering tests",
            testInitializationResult.Errors.Count);

        var testDiscoveryExceptions = new List<Exception>();
        foreach (var (reason, names) in testInitializationResult.Errors)
        {
            _logger.Log(LogLevel.Error, "{Reason}", reason);
            foreach (var testName in names)
            {
                _logger.Log(LogLevel.Error, "--- {TestName}", testName);
                testDiscoveryExceptions.Add(new SailfishException($"Test: {testName} - Error: {reason}"));
            }
        }

        return SailfishRunResult.CreateResult(Array.Empty<IClassExecutionSummary>(), testDiscoveryExceptions);
    }

    private static int? TryParseSeed(Sailfish.Extensions.Types.OrderedDictionary args)
    {
        try
        {
            foreach (var kv in args)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (string.Equals(key, "seed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "randomseed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "rng", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var s)) return s;
                }
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private TestInitializationResult CollectTests(IEnumerable<string> testNames, IEnumerable<Type> locationTypes)
    {
        var perfTests = _testCollector.CollectTestTypes(locationTypes);
        return _testFilter.FilterAndValidate(perfTests, testNames);
    }
}