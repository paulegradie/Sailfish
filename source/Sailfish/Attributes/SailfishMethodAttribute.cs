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
    ///     Opts this method into a named comparison group. Methods that share an explicit
    ///     <see cref="ComparisonGroup"/> within a single test class are compared against each other.
    ///     When left <c>null</c> (default), the method joins the implicit class-wide comparison group —
    ///     unless the enclosing class sets <c>[Sailfish(DisableComparison = true)]</c>, in which case
    ///     the method runs without producing comparison output.
    /// </summary>
    /// <remarks>
    ///     Most classes never need to set this — every method in a <c>[Sailfish]</c> class is compared
    ///     by default. Setting <see cref="ComparisonGroup"/> is the advanced path for classes that need
    ///     multiple distinct comparison groups (e.g. sorting algorithms in one group and hashing
    ///     algorithms in another).
    ///
    ///     Example:
    ///     <code>
    ///     [Sailfish]
    ///     public class MixedBenchmarks
    ///     {
    ///         [SailfishMethod(ComparisonGroup = "Sort", IsBaseline = true)]
    ///         public void QuickSort() { /* ... */ }
    ///
    ///         [SailfishMethod(ComparisonGroup = "Sort")]
    ///         public void BubbleSort() { /* ... */ }
    ///
    ///         [SailfishMethod(ComparisonGroup = "Hash")]
    ///         public void Md5() { /* ... */ }
    ///
    ///         [SailfishMethod(ComparisonGroup = "Hash")]
    ///         public void Sha256() { /* ... */ }
    ///     }
    ///     </code>
    /// </remarks>
    public string? ComparisonGroup { get; set; }

    /// <summary>
    ///     When <c>true</c>, marks this method as the baseline of its comparison group.
    ///     The "comparison group" is the explicit <see cref="ComparisonGroup"/> when set, otherwise
    ///     the enclosing class's implicit class-wide group.
    ///     All other methods in the group are compared against the baseline (N−1 comparisons).
    ///     When no method in a group sets <see cref="IsBaseline"/>, every pair is compared (N×N).
    /// </summary>
    /// <remarks>
    ///     At most one method per group may set <c>IsBaseline = true</c>; the SF1301 analyzer
    ///     enforces this at build time and the runtime falls back to N×N if violated.
    ///     Setting <see cref="IsBaseline"/> on a method that isn't in any comparison group
    ///     (e.g. its class is <c>[Sailfish(DisableComparison = true)]</c> and the method has no
    ///     explicit <see cref="ComparisonGroup"/>) is reported by the SF1300 analyzer.
    /// </remarks>
    public bool IsBaseline { get; set; }
}