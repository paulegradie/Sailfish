using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

/// <summary>
/// Attribute to be placed on one or more methods whose execution time will be tracked
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <note>The SailfishMethod is called NumWarmupIterations times without time tracking, and then called SampleSize with time tracking</note>
/// <remarks>Use 'nameof(DecoratedMethodName)' to request the application of method and iteration lifecycle methods</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class SailfishMethodAttribute : Attribute
{
    /// <summary>
    /// Sets the order of execution for a SailfishMethod within the Sailfish class.
    /// Ordered methods are always executed before unordered methods.
    /// Order of unordered methods is not guaranteed.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Indicates whether the Sailfish method is disabled.
    /// </summary>
    /// <value><c>true</c> if the test is disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets/Sets whether to disable complexity analysis for this method logic
    /// </summary>
    public bool DisableComplexity { get; set; }

    /// <summary>
    /// Gets/Sets whether to disable overhead estimation for the method
    /// Description:
    ///   - Disables the attempt to calibrate distributions for endemic noise
    ///   - When disabled, test iterations are much quicker
    /// Note:
    ///   - This sometimes causes negative deltas for small measurements
    ///   - Sailfish is not particularly well suited for micro-measurements 
    /// </summary>
    public bool DisableOverheadEstimation { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishMethodAttribute"/> class.
    /// </summary>
    /// <param name="disabled">Whether or not to ignore the given test method</param>
    /// <param name="disableComplexity">Whether or not to disable complexity analysis for this method</param>
    public SailfishMethodAttribute()
    {
    }
}