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
[AttributeUsage(AttributeTargets.Property)]
public sealed class SailfishVariableAttribute : Attribute, ISailfishVariableAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified integer values.
    /// </summary>
    /// <param name="n">A params array of integer values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params int[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified string values.
    /// </summary>
    /// <param name="n">A params array of string values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params string[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified double values.
    /// </summary>
    /// <param name="n">A params array of double values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params double[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified float values.
    /// </summary>
    /// <param name="n">A params array of float values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params float[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified long values.
    /// </summary>
    /// <param name="n">A params array of long values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params long[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified decimal values.
    /// </summary>
    /// <param name="n">A params array of decimal values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params decimal[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified boolean values.
    /// </summary>
    /// <param name="n">A params array of boolean values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params bool[] n)
    {
        if (n.Length == 0)
            throw new SailfishException(
                $"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n.Cast<object>());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified integer values and the
    ///     option to best fit the test method to a complexity curve.
    /// </summary>
    /// <param name="scaleFish">Boolean to enable complexity estimate feature</param>
    /// <param name="n">A params array of integer values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute(bool scaleFish, [MinLength(3)] params int[] n) : this(n)
    {
        UseScaleFish = scaleFish;
        if (UseScaleFish && n.Length < 3)
            throw new SailfishException(
                "Complexity estimation requires at least 3 variable values for n. Accuracy positively correlates with the number and breath of values for n.");
    }

    /// <summary>
    ///     Gets the list of values used as variables within the test.
    /// </summary>
    private List<object> N { get; } = new();

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
}