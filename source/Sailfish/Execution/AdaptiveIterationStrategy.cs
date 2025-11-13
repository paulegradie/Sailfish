using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;

namespace Sailfish.Execution;

/// <summary>
/// Adaptive iteration strategy that continues execution until statistical convergence is achieved.
/// Uses coefficient of variation to determine when sufficient samples have been collected.
/// </summary>
internal class AdaptiveIterationStrategy : IIterationStrategy
{
    private readonly ILogger logger;
    private readonly IStatisticalConvergenceDetector convergenceDetector;

    /// <summary>
    /// Initializes a new instance of the AdaptiveIterationStrategy class.
    /// </summary>
    /// <param name="logger">Logger for iteration progress and convergence information</param>
    /// <param name="convergenceDetector">Detector for statistical convergence analysis</param>
    public AdaptiveIterationStrategy(
        ILogger logger,
        IStatisticalConvergenceDetector convergenceDetector)
    {
        this.logger = logger;
        this.convergenceDetector = convergenceDetector;
    }

    /// <summary>
    /// Executes test iterations until statistical convergence is achieved or maximum iterations reached.
    /// </summary>
    /// <param name="testInstanceContainer">The test instance container with the test to execute</param>
    /// <param name="executionSettings">The execution settings containing adaptive sampling configuration</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution</param>
    /// <returns>An IterationResult containing execution outcome and convergence information</returns>
    public async Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken)
    {
        var minIterations = executionSettings.MinimumSampleSize;
        var maxIterations = executionSettings.MaximumSampleSize;
        var targetCV = executionSettings.TargetCoefficientOfVariation;
        var confidenceLevel = executionSettings.ConfidenceLevel;
        var maxCiWidth = executionSettings.MaxConfidenceIntervalWidth;
        var effectiveMin = minIterations;

        var iteration = 0;
        var timer = testInstanceContainer.CoreInvoker.GetPerformanceResults();
        var timeBudget = executionSettings.MaxMeasurementTimePerMethod;
        var testStart = timer.GetIterationStartTime();
        ConvergenceResult? convergenceResult = null;

        // Execute minimum iterations first
        for (iteration = 0; iteration < minIterations; iteration++)
        {
            if (timeBudget.HasValue)
            {
                var elapsed = DateTimeOffset.Now - testStart;
                if (elapsed >= timeBudget.Value)
                {
                    logger.Log(LogLevel.Warning,
                        "      ---- Stopping early: MaxMeasurementTimePerMethod {MaxMs} ms exceeded after {ElapsedMs} ms during minimum phase",
                        timeBudget.Value.TotalMilliseconds, elapsed.TotalMilliseconds);
                    return new IterationResult
                    {
                        IsSuccess = true,
                        TotalIterations = iteration,
                        ConvergedEarly = false,
                        ConvergenceReason = $"Time budget exceeded after {elapsed.TotalMilliseconds:F0} ms"
                    };
                }
            }

            logger.Log(LogLevel.Information,
                "      ---- iteration {CurrentIteration} (minimum phase)",
                iteration + 1);

            try
            {
                await ExecuteSingleIteration(testInstanceContainer, cancellationToken);
            }
            catch (Exception ex)
            {
                return new IterationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    TotalIterations = iteration
                };
            }
        }

        // Perform an initial convergence check after the minimum phase
        var initialSamples = GetCurrentSamples(testInstanceContainer);

        // NEW: Use a pilot analysis to tune thresholds (without changing min/max iteration counts)
        // Keeps backward compatibility with existing tests and behavior
        try
        {
            var selector = new AdaptiveParameterSelector();
            var selected = selector.Select(initialSamples, executionSettings);
            targetCV = selected.TargetCoefficientOfVariation;
            maxCiWidth = selected.MaxConfidenceIntervalWidth;
            effectiveMin = Math.Max(minIterations, selected.RecommendedMinimumSampleSize);
            logger.Log(LogLevel.Information, "      ---- Adaptive tuning: {Category} -> MinN={MinN}, TargetCV={TargetCV:F3}, MaxCI={MaxCI:F3}", selected.Category, effectiveMin, targetCV, maxCiWidth);
            if (!string.IsNullOrWhiteSpace(selected.SelectionReason))
            {
                logger.Log(LogLevel.Information, "      ---- Reason: {SelectionReason}", selected.SelectionReason);
            }
        }
        catch
        {
            // Best-effort; ignore selector failures and proceed with original thresholds
        }


            // Budget-aware precision controller (opt-in)
            try
            {
                var controller = new PrecisionTimeBudgetController();
                var adjusted = controller.Adjust(initialSamples, executionSettings, testStart, DateTimeOffset.Now);
                if (adjusted.TargetCV > targetCV || adjusted.MaxConfidenceIntervalWidth > maxCiWidth)
                {
                    logger.Log(LogLevel.Information,
                        "      ---- Budget controller: remaining={RemainingMs:F1}ms, est/iter={PerIterMs:F2}ms, TargetCV {OldCv:F3}->{NewCv:F3}, MaxCI {OldCi:F3}->{NewCi:F3}",
                        adjusted.RemainingMs, adjusted.PerIterMs, targetCV, adjusted.TargetCV, maxCiWidth, adjusted.MaxConfidenceIntervalWidth);
                    targetCV = adjusted.TargetCV;
                    maxCiWidth = adjusted.MaxConfidenceIntervalWidth;
                }
            }
            catch
            {
                // Best-effort; ignore controller failures
            }

        convergenceResult = convergenceDetector.CheckConvergence(
            initialSamples, targetCV, maxCiWidth, confidenceLevel, effectiveMin);

        if (convergenceResult.HasConverged)
        {
            logger.Log(LogLevel.Information,
                "      ---- Converged after {TotalIterations} iterations: {Reason}",
                iteration, convergenceResult.Reason);
        }
        else
        {
            // Execute more iterations up to the cap, checking convergence AFTER each iteration
            while (iteration < maxIterations)
            {
                if (timeBudget.HasValue)
                {
                    var elapsed = DateTimeOffset.Now - testStart;
                    if (elapsed >= timeBudget.Value)
                    {
                        logger.Log(LogLevel.Warning,
                            "      ---- Stopping early: MaxMeasurementTimePerMethod {MaxMs} ms exceeded after {ElapsedMs} ms",
                            timeBudget.Value.TotalMilliseconds, elapsed.TotalMilliseconds);
                        return new IterationResult
                        {
                            IsSuccess = true,
                            TotalIterations = iteration,
                            ConvergedEarly = false,
                            ConvergenceReason = $"Time budget exceeded after {elapsed.TotalMilliseconds:F0} ms"
                        };
                    }
                }

                logger.Log(LogLevel.Information,
                    "      ---- iteration {CurrentIteration} (CV: {CurrentCV:F4}, target: {TargetCV:F4})",
                    iteration + 1, convergenceResult.CurrentCoefficientOfVariation, targetCV);

                try
                {
                    await ExecuteSingleIteration(testInstanceContainer, cancellationToken);
                    iteration++;
                }
                catch (Exception ex)
                {
                    return new IterationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        TotalIterations = iteration
                    };
                }

                // Re-evaluate convergence on the latest samples (including the last allowed iteration)
                var currentSamples = GetCurrentSamples(testInstanceContainer);
                convergenceResult = convergenceDetector.CheckConvergence(
                    currentSamples, targetCV, maxCiWidth, confidenceLevel, effectiveMin);

                if (convergenceResult.HasConverged)
                {
                    logger.Log(LogLevel.Information,
                        "      ---- Converged after {TotalIterations} iterations: {Reason}",
                        iteration, convergenceResult.Reason);
                    break;
                }
            }
        }

        var convergedEarly = convergenceResult?.HasConverged == true && iteration < maxIterations;

        if (convergenceResult?.HasConverged != true && iteration >= maxIterations)
        {
            logger.Log(LogLevel.Warning,
                "      ---- Reached maximum iterations ({MaxIterations}) without convergence. CV: {CurrentCV:F4}",
                maxIterations, convergenceResult?.CurrentCoefficientOfVariation ?? 0);
        }

        return new IterationResult
        {
            IsSuccess = true,
            TotalIterations = iteration,
            ConvergedEarly = convergedEarly,
            ConvergenceReason = convergenceResult?.Reason ?? $"Completed {iteration} iterations"
        };
    }

    /// <summary>
    /// Executes a single test iteration including setup, execution, and teardown.
    /// </summary>
    private async Task ExecuteSingleIteration(
        TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
        var opi = Math.Max(1, testInstanceContainer.ExecutionSettings.OperationsPerInvoke);
        if (opi > 1)
        {
            await testInstanceContainer.CoreInvoker.ExecutionMethodWithOperationsPerInvoke(opi, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken).ConfigureAwait(false);
        }
        await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the current performance samples from the test instance container.
    /// </summary>
    private double[] GetCurrentSamples(TestInstanceContainer testInstanceContainer)
    {
        var timer = testInstanceContainer.CoreInvoker.GetPerformanceResults();
        return timer.ExecutionIterationPerformances
            .Select(x => x.GetDurationFromTicks().NanoSeconds.Duration)
            .ToArray();
    }
}
