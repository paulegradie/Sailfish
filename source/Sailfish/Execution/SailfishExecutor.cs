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
        _classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        _executionSummaryWriter = executionSummaryWriter;
        _logger = logger;
        _mediator = mediator;
        _runSettings = runSettings;
        _sailDiff = sailDiff;
        _sailFishTestExecutor = sailFishTestExecutor;
        _scaleFish = scaleFish;
        _testCollector = testCollector;
        _testFilter = testFilter;
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

            // Benchmarks are measured at this point. From here on every artifact-writing / analysis step runs
            // inside an error boundary: a failure in post-measurement work (serialization, reporting, SailDiff,
            // ScaleFish) must never lose the collected timings or crash the host. Previously an unhandled
            // exception here aborted the process with exit 134. Each stage degrades independently and the
            // failure is surfaced on the result (a graceful non-zero outcome) rather than thrown.
            var analysisExceptions = new List<Exception>();

            await RunPostMeasurementStage(
                "write execution summaries",
                () => _executionSummaryWriter.Write(classExecutionSummaries, cancellationToken),
                analysisExceptions).ConfigureAwait(false);

            await RunPostMeasurementStage(
                "publish test-run-completed notification",
                () => _mediator.Publish(new TestRunCompletedNotification(classExecutionSummaries.ToTrackingFormat()), cancellationToken),
                analysisExceptions).ConfigureAwait(false);

            if (_runSettings.RunSailDiff)
                await RunPostMeasurementStage("SailDiff analysis", () => _sailDiff.Analyze(cancellationToken), analysisExceptions).ConfigureAwait(false);

            if (_runSettings.RunScaleFish)
                await RunPostMeasurementStage("ScaleFish analysis", () => _scaleFish.Analyze(cancellationToken), analysisExceptions).ConfigureAwait(false);

            var exceptions = classExecutionSummaries
                .SelectMany(classExecutionSummary =>
                    classExecutionSummary
                        .CompiledTestCaseResults
                        .Where(e => e.Exception is not null)
                        .Select(c => c.Exception))
                .Cast<Exception>()
                .ToList();

            // Surface post-measurement failures alongside measurement exceptions so a run whose analysis
            // failed reports a graceful, non-crashing failure (IsValid == false) while still returning the
            // collected timings and whatever artifacts were produced.
            exceptions.AddRange(analysisExceptions);

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

    /// <summary>
    ///     Runs a single post-measurement stage (artifact write, notification publish, or an analyzer) inside
    ///     an error boundary. A throw is logged as a structured error and recorded in
    ///     <paramref name="analysisExceptions" /> so the stage fails soft — the collected timings survive,
    ///     the remaining stages still run, and the process is never aborted. Cancellation is a control-flow
    ///     signal rather than an analysis failure, so it is allowed to propagate.
    /// </summary>
    private async Task RunPostMeasurementStage(string stageName, Func<Task> stage, List<Exception> analysisExceptions)
    {
        try
        {
            await stage().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            analysisExceptions.Add(ex);
            _logger.Log(
                LogLevel.Error,
                ex,
                "Post-measurement stage '{Stage}' failed after benchmarks were measured. The collected timings are preserved and the remaining stages continue; this step's artifacts/analysis were skipped.",
                stageName);
        }
    }

    private static int? TryParseSeed(Extensions.Types.OrderedDictionary args)
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