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
/// <exception cref="SailfishException">Thrown when no values are provided.</exception>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SailfishRangeVariableAttribute : Attribute, ISailfishVariableAttribute
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
    public SailfishRangeVariableAttribute(int start, int count, int step = 1)
    {
        N = Range(start, count, step).Cast<object>();
    }

    /// <summary>
    ///     Initializes a new instance with geometrically-spaced values between <paramref name="start"/> and
    ///     <paramref name="end"/> (inclusive). Geometric spacing is the recommended layout for ScaleFish
    ///     complexity probes — equally-spaced log-x values give substantially better discrimination
    ///     between complexity classes than linear spacing.
    /// </summary>
    /// <param name="scaleFish">Boolean to enable complexity estimate feature.</param>
    /// <param name="start">Smallest value (must be &gt; 0 for Geometric spacing).</param>
    /// <param name="end">Largest value (rounded to nearest int after geometric interpolation).</param>
    /// <param name="count">Number of values to generate (≥ 2). For ScaleFish, at least 3 is required.</param>
    /// <param name="spacing">Linear or Geometric distribution between start and end.</param>
    public SailfishRangeVariableAttribute(bool scaleFish, int start, int end, int count, RangeSpacing spacing)
        : this(start, end, count, spacing)
    {
        UseScaleFish = scaleFish;
        if (UseScaleFish && count < 3)
            throw new SailfishException(
                "Complexity estimation requires at least 3 variable values for n. Accuracy positively correlates with the number and breath of values for n.");
    }

    /// <summary>
    ///     Initializes a new instance with values distributed between <paramref name="start"/> and
    ///     <paramref name="end"/> according to <paramref name="spacing"/>.
    /// </summary>
    public SailfishRangeVariableAttribute(int start, int end, int count, RangeSpacing spacing)
    {
        N = SpacedRange(start, end, count, spacing).Cast<object>();
    }

    /// <summary>
    ///     Gets the list of values used as variables within the test.
    /// </summary>
    private IEnumerable<object> N { get; }

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

    internal static IEnumerable<int> SpacedRange(int start, int end, int count, RangeSpacing spacing)
    {
        if (count < 2) throw new ArgumentException("count must be at least 2");
        if (end == start) throw new ArgumentException("end must differ from start");

        if (spacing == RangeSpacing.Linear)
        {
            var step = (end - start) / (double)(count - 1);
            for (var i = 0; i < count; i++)
                yield return (int)Math.Round(start + i * step);
            yield break;
        }

        if (start <= 0)
            throw new ArgumentException("start must be > 0 for geometric spacing");
        if (end <= start)
            throw new ArgumentException("end must be > start for geometric spacing");

        var ratio = Math.Pow((double)end / start, 1.0 / (count - 1));
        var previous = int.MinValue;
        for (var i = 0; i < count; i++)
        {
            var raw = start * Math.Pow(ratio, i);
            var v = (int)Math.Round(raw);
            if (v <= previous) v = previous + 1; // keep strictly increasing if rounding collapses two adjacent points
            previous = v;
            yield return v;
        }
    }
}