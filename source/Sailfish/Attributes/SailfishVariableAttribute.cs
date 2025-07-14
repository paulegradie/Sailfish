using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Sailfish.Exceptions;
using System.Collections;

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
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified values.
    /// </summary>
    /// <param name="n">A params array of values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute([MinLength(1)] params object[] n)
    {
        if (n.Length == 0) throw new SailfishException($"No values were provided to the {nameof(SailfishVariableAttribute)} attribute.");
        N.AddRange(n);
    }

    public SailfishVariableAttribute(Type type)
    {
        variablesProvidingType = type;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishVariableAttribute" /> class with the specified values and the
    ///     option to best fit the test method to a complexity curve.
    /// </summary>
    /// <param name="scaleFish">Boolean to enable complexity estimate feature</param>
    /// <param name="n">A params array of values to be used as variables within the test.</param>
    /// <exception cref="SailfishException">Thrown when no values are provided.</exception>
    public SailfishVariableAttribute(bool scaleFish, [MinLength(3)] params int[] n) : this(n.Cast<object>().ToArray())
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
    
    /// <summary>
    ///     
    /// </summary>
    private Type? variablesProvidingType { get; }

    private bool UseScaleFish { get; }

    /// <summary>
    ///     Retrieves the variables as an enumerable.
    /// </summary>
    /// <returns>An enumerable of the variables.</returns>
    public IEnumerable<object> GetVariables()
    {
        if (N.Count == 1 && N.First() is Type type)
        {
            if (typeof(ISailfishVariablesProvider).IsAssignableFrom(type))
            {
                var instance = (ISailfishVariablesProvider) Activator.CreateInstance(type);
                return instance.Variables;
            }

        }
        return N.ToArray();
    }

    private static IEnumerable<object> CollectVariablesFromVariablesProvider(Type variablesProvidingType)
    {
        if (variablesProvidingType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISailfishVariablesProvider<>)))
        {
            var instance = Activator.CreateInstance(variablesProvidingType);

            if (instance == null)
            {
                throw new Exception($"Could not construct instance of {variablesProvidingType}.");
            }
            
            var method = instance.GetType().GetMethod(nameof(ISailfishVariablesProvider<string>.Variables));
            if (method == null)
            {
                throw new Exception($"Could not find Variables() method on type {variablesProvidingType}.");
            }

            return (IEnumerable<object>) method.Invoke(instance, null);
        }

        throw new Exception($"Type {variablesProvidingType} does not implement {typeof(ISailfishVariablesProvider<>).FullName}.");
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