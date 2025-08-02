namespace Sailfish.TestAdapter.Queue.Configuration;

/// <summary>
/// Defines strategies for detecting when to perform method comparisons.
/// </summary>
public enum ComparisonDetectionStrategy
{
    /// <summary>
    /// Detect full class execution by counting test cases in the batch.
    /// If all methods from a class are present, perform comparisons.
    /// </summary>
    ByTestCaseCount,
    
    /// <summary>
    /// Always perform comparisons when comparison groups are complete,
    /// regardless of execution context.
    /// </summary>
    Always,
    
    /// <summary>
    /// Never perform comparisons (disabled).
    /// </summary>
    Never
}
