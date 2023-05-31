using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sailfish.Exceptions;

namespace Sailfish.Attributes;

/// <summary>
/// An attribute to decorate a property that will be referenced within the test.
/// A unique execution set of the performance tests is executed for each value provided,
/// where an execution set is the total number of executions specified by the SailfishAttribute.
/// </summary>
/// <remarks>
/// This attribute should be applied to properties. It has no effect when applied to fields.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class SailfishVariableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishVariableAttribute"/> class with the specified values.
    /// </summary>
    /// <param name="n">A params array of values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params object[] n)
    {
        if (n.Length == 0)
        {
            throw new SailfishException($"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        }

        N.AddRange(n);
    }

    /// <summary>
    /// Gets the list of values used as variables within the test.
    /// </summary>
    public List<object> N { get; } = new();

    /// <summary>
    /// Retrieves the variables as an enumerable.
    /// </summary>
    /// <returns>An enumerable of the variables.</returns>
    public IEnumerable<object> GetVariables()
    {
        return N.ToArray();
    }
}