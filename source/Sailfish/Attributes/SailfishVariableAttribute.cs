using System;
using Sailfish.Exceptions;

namespace Sailfish.Attributes;

/// <summary>
///     This is used to decorate a property that will be referenced within the test.
///     A unique execution set of the performance tests is executed for each value provided,
///     where an execution set is the total number of executions specified by the
///     Sailfish attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SailfishVariableAttribute : Attribute
{
    public SailfishVariableAttribute(params int[] n)
    {
        if (n.Length == 0) throw new SailfishException("No values were provided to the IterationVariable attribute.");
        N = n;
    }

    public int[] N { get; set; }
}