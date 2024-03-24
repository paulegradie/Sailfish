using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Sailfish.Exceptions;

namespace Sailfish.Attributes;

/// <summary>
///     An attribute to decorate a property that will be referenced within the test.
///     A unique execution set of the performance tests is executed for each value provided,
///     where an execution set is the total number of executions specified by the SailfishAttribute.
/// </summary>
/// <remarks>
///     This attribute should be applied to public properties. It has no effect when applied to fields.
/// </remarks>
/// <remarks>
///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified values.
/// </remarks>
/// <param name="start">Int value to start the range</param>
/// <param name="count">Number of values to create</param>
/// <param name="step">Step between values</param>
/// <exception cref="SailfishException">Thrown when no values are provided.</exception>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SailfishRangeVariableAttribute(int start, int count, int step = 1) : Attribute, ISailfishVariableAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified values and the
    ///     option to best fit the test method to a complexity curve.
    /// </summary>
    /// <param name="scaleFish">Boolean to enable complexity extimate feature</param>
    /// <param name="start">Int value to start the range</param>
    /// <param name="count">Number of values to create</param>
    /// <param name="step">Step between values</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishRangeVariableAttribute(bool scaleFish, int start, int count, int step = 1) : this(start, count, step)
    {
        UseScaleFish = scaleFish;
    }

    /// <summary>
    ///     Gets the list of values used as variables within the test.
    /// </summary>
    private IEnumerable<object> N { get; } = Range(start, count, step).Cast<object>();

    private bool UseScaleFish { get; }

    /// <summary>
    ///     Retrieves the variables as an enumerable.
    /// </summary>
    /// <returns>An enumerable of the variables.</returns>
    public IEnumerable<object> GetVariables()
    {
        return N.ToArray();
    }

    /// <summary>
    ///     Retrieves bool indicating if this attribute should be used for complexity estimation
    /// </summary>
    /// <returns>bool</returns>
    public bool IsScaleFishVariable()
    {
        return UseScaleFish;
    }

    private static IEnumerable<int> Range(int start, [Range(1, int.MaxValue)] int count, [Range(1, int.MaxValue)] int step)
    {
        if (count < 1 || step < 1) throw new ArgumentException("Count and step must be positive.");

        var current = start;
        for (var i = 0; i < count; i++)
        {
            yield return current;
            current += step;
        }
    }
}