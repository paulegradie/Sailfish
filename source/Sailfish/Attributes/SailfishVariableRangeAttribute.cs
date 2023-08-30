using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Sailfish.Exceptions;

namespace Sailfish.Attributes;

/// <summary>
/// An attribute to decorate a property that will be referenced within the test.
/// A unique execution set of the performance tests is executed for each value provided,
/// where an execution set is the total number of executions specified by the SailfishAttribute.
/// </summary>
/// <remarks>
/// This attribute should be applied to public properties. It has no effect when applied to fields.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SailfishVariableRangeAttribute : Attribute, ISailfishVariableAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishVariableAttribute"/> class with the specified values.
    /// </summary>
    /// <param name="start">Int value to start the range</param>
    /// <param name="count">Number of values to create</param>
    /// <param name="step">Step between values</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableRangeAttribute(int start, int count, int step = 1)
    {
        N = Range(start, count, step).Cast<object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishVariableAttribute"/> class with the specified values and the option to best fit the test method to a complexity curve.
    /// </summary>
    /// <param name="complexity">Boolean to enable complexity extimate feature</param>
    /// <param name="start">Int value to start the range</param>
    /// <param name="count">Number of values to create</param>
    /// <param name="step">Step between values</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableRangeAttribute(bool complexity, int start, int count, int step = 1) : this(start, count, step)
    {
        EstimateComplexity = complexity;
    }

    /// <summary>
    /// Gets the list of values used as variables within the test.
    /// </summary>
    private IEnumerable<object> N { get; }

    private bool EstimateComplexity { get; set; }

    /// <summary>
    /// Retrieves the variables as an enumerable.
    /// </summary>
    /// <returns>An enumerable of the variables.</returns>
    public IEnumerable<object> GetVariables()
    {
        return N.ToArray();
    }

    /// <summary>
    /// Retrieves bool indicating if this attribute should be used for complexity estimation
    /// </summary>
    /// <returns>bool</returns>
    public bool IsComplexityVariable()
    {
        return EstimateComplexity;
    }

    private static IEnumerable<int> Range(int start, [Range(0, int.MaxValue)] int count, [Range(0, int.MaxValue)] int step)
    {
        if (count <= 0 || step <= 0)
        {
            throw new ArgumentException("Count and step must be positive.");
        }

        var current = start;

        for (var i = 0; i < count; i++)
        {
            yield return current;
            current += step;
        }
    }
}