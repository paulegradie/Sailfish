using System;

namespace Sailfish.Attributes;

/// <summary>
/// Attribute to be placed on one or more methods whose execution time will be tracked
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <note>The SailfishMethod is called NumWarmupIterations times without time tracking, and then called NumIterations with time tracking</note>
/// <remarks>Use 'nameof(DecoratedMethodName)' to request the application of method and iteration lifecycle methods</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class SailfishMethodAttribute : Attribute
{
    /// <summary>
    /// Indicates whether the Sailfish method is disabled.
    /// </summary>
    /// <value><c>true</c> if the test is disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; }

    /// <summary>
    /// Gets or sets a va
    /// </summary>
    public bool DisableComplexity { get; }

    public bool DisableOverheadEstimation { get; }

    internal SailfishMethodAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishMethodAttribute"/> class.
    /// </summary>
    /// <param name="disabled">Whether or not to ignore the given test method</param>
    /// <param name="disableComplexity">Whether or not to disable complexity analysis for this method</param>
    public SailfishMethodAttribute(bool disabled = false, bool disableComplexity = false, bool disableOverheadEstimation = false)
    {
        Disabled = disabled;
        DisableComplexity = disableComplexity;
        DisableOverheadEstimation = disableOverheadEstimation;
    }
}