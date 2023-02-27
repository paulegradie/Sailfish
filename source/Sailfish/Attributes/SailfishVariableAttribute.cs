using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sailfish.Exceptions;

namespace Sailfish.Attributes;

/// <summary>
///     This is used to decorate a property that will be referenced within the test.
///     A unique execution set of the performance tests is executed for each value provided,
///     where an execution set is the total number of executions specified by the
///     Sailfish attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SailfishVariableAttribute : Attribute
{
    public SailfishVariableAttribute([MinLength(1)] params object[] n)
    {
        if (n.Length == 0) throw new SailfishException($"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n);
    }

    public List<object> N { get; } = new();

    public IEnumerable<object> GetVariables()
    {
        return N.ToArray();
    }
}
