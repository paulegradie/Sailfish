using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

/// <summary>
///     Attribute to be placed on one or more methods whose execution time will be tracked
///     See:
///     <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">
///         Sailfish Lifecycle Method
///         Attributes
///     </a>
/// </summary>
/// <note>
///     The SailfishMethod is called NumWarmupIterations times without time tracking, and then called SampleSize with
///     time tracking
/// </note>
/// <remarks>Use 'nameof(DecoratedMethodName)' to request the application of method and iteration lifecycle methods</remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SailfishMethodAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishMethodAttribute" /> class.
    /// </summary>
    /// <param name="disabled">Whether or not to ignore the given test method</param>
    /// <param name="disableComplexity">Whether or not to disable complexity analysis for this method</param>
    public SailfishMethodAttribute()
    {
    }

    /// <summary>
    ///     Sets the order of execution for a SailfishMethod within the Sailfish class.
    ///     Ordered methods are always executed before unordered methods.
    ///     Order of unordered methods is not guaranteed.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    ///     Indicates whether the Sailfish method is disabled.
    /// </summary>
    /// <value><c>true</c> if the test is disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets/Sets whether to disable complexity analysis for this method logic
    /// </summary>
    public bool DisableComplexity { get; set; }

    /// <summary>
    ///     Gets/Sets whether to disable overhead estimation for the method
    ///     Description:
    ///     - Disables the attempt to calibrate distributions for endemic noise
    ///     - When disabled, test iterations are much quicker
    ///     Note:
    ///     - This sometimes causes negative deltas for small measurements
    ///     - Sailfish is not particularly well suited for micro-measurements
    /// </summary>
    public bool DisableOverheadEstimation { get; set; }

    /// <summary>
    ///     Opts this method into a named comparison group. All methods in the same group
    ///     within a single test class are compared against each other when the class runs.
    ///     Leave <c>null</c> (default) to exclude the method from comparison entirely.
    /// </summary>
    /// <remarks>
    ///     Comparison is always opt-in. A method without <see cref="ComparisonGroup"/> set
    ///     simply runs as a normal Sailfish method with no comparison output.
    ///
    ///     Combine with <see cref="IsBaseline"/> to designate a baseline; without one, all
    ///     methods in the group are compared pairwise (N×N matrix).
    ///
    ///     Example:
    ///     <code>
    ///     [SailfishMethod(ComparisonGroup = "Sort", IsBaseline = true)]
    ///     public void QuickSort() { /* ... */ }
    ///
    ///     [SailfishMethod(ComparisonGroup = "Sort")]
    ///     public void BubbleSort() { /* ... */ }
    ///     </code>
    /// </remarks>
    public string? ComparisonGroup { get; set; }

    /// <summary>
    ///     When <c>true</c>, marks this method as the baseline of its <see cref="ComparisonGroup"/>.
    ///     All other methods in the group are compared against the baseline (N−1 comparisons).
    ///     When no method in a group sets <see cref="IsBaseline"/>, every pair is compared (N×N).
    /// </summary>
    /// <remarks>
    ///     At most one method per <c>(class, ComparisonGroup)</c> may set <c>IsBaseline = true</c>;
    ///     the SF1301 analyzer enforces this at build time and the runtime falls back to N×N if violated.
    ///     <see cref="IsBaseline"/> requires <see cref="ComparisonGroup"/> to be set (enforced by SF1300).
    /// </remarks>
    public bool IsBaseline { get; set; }
}