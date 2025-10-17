using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;

namespace Sailfish.Execution;

/// <summary>
/// Fixed iteration strategy that executes a predetermined number of iterations.
/// This maintains the existing Sailfish behavior for backward compatibility.
/// </summary>
internal class FixedIterationStrategy : IIterationStrategy
{
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the FixedIterationStrategy class.
    /// </summary>
    /// <param name="logger">Logger for iteration progress</param>
    public FixedIterationStrategy(ILogger logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Executes a fixed number of test iterations as specified in the execution settings.
    /// </summary>
    /// <param name="testInstanceContainer">The test instance container with the test to execute</param>
    /// <param name="executionSettings">The execution settings containing the sample size</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution</param>
    /// <returns>An IterationResult containing execution outcome</returns>
    public async Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken)
    {
        var iterations = executionSettings.SampleSize;
        
        for (var i = 0; i < iterations; i++)
        {
            logger.Log(LogLevel.Information, 
                "      ---- iteration {CurrentIteration} of {TotalIterations}", 
                i + 1, iterations);

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
                await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken).ConfigureAwait(false);
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new IterationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    TotalIterations = i
                };
            }
        }

        return new IterationResult
        {
            IsSuccess = true,
            TotalIterations = iterations,
            ConvergedEarly = false,
            ConvergenceReason = $"Completed {iterations} fixed iterations"
        };
    }
}
