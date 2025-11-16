using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
/// Strategy interface for different iteration approaches in performance testing.
/// Allows switching between fixed iteration count and adaptive sampling strategies.
/// </summary>
internal interface IIterationStrategy
{
    /// <summary>
    /// Executes test iterations according to the strategy's approach.
    /// </summary>
    /// <param name="testInstanceContainer">The test instance container with the test to execute</param>
    /// <param name="executionSettings">The execution settings containing configuration</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution</param>
    /// <returns>An IterationResult containing execution outcome and statistics</returns>
    Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of an iteration strategy execution.
/// Contains information about the execution outcome and any convergence details.
/// </summary>
internal class IterationResult
{
    /// <summary>
    /// Gets whether the iteration execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Gets the total number of iterations executed.
    /// </summary>
    public int TotalIterations { get; init; }
    
    /// <summary>
    /// Gets whether the strategy converged early (applicable to adaptive sampling).
    /// </summary>
    public bool ConvergedEarly { get; init; }
    
    /// <summary>
    /// Gets the reason for convergence or completion.
    /// </summary>
    public string? ConvergenceReason { get; init; }
}
