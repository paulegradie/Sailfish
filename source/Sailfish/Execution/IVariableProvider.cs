using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;

namespace Sailfish.Execution;

/// <summary>
/// Interface for providing variables to Sailfish tests, supporting both attribute-based and interface-based variable providers
/// </summary>
internal interface IVariableProvider
{
    /// <summary>
    /// Gets the variables for test execution
    /// </summary>
    /// <returns>An enumerable of variable values</returns>
    IEnumerable<object> GetVariables();

    /// <summary>
    /// Indicates if this variable provider should be used for complexity estimation (ScaleFish)
    /// </summary>
    /// <returns>True if this should be used for complexity estimation</returns>
    bool IsScaleFishVariable();
}

/// <summary>
/// Variable provider for attribute-based variables (SailfishVariableAttribute, SailfishRangeVariableAttribute)
/// </summary>
internal class AttributeVariableProvider : IVariableProvider
{
    private readonly ISailfishVariableAttribute attribute;

    public AttributeVariableProvider(ISailfishVariableAttribute attribute)
    {
        this.attribute = attribute;
    }

    public IEnumerable<object> GetVariables()
    {
        return attribute.GetVariables();
    }

    public bool IsScaleFishVariable()
    {
        return attribute.IsScaleFishVariable();
    }
}

/// <summary>
/// Variable provider for interface-based complex variables (ISailfishComplexVariableProvider)
/// </summary>
internal class ComplexVariableProvider : IVariableProvider
{
    private readonly System.Type propertyType;

    public ComplexVariableProvider(System.Type propertyType)
    {
        this.propertyType = propertyType;
    }

    public IEnumerable<object> GetVariables()
    {
        return GetComplexVariables(propertyType);
    }

    public bool IsScaleFishVariable()
    {
        // Complex variables are not used for complexity estimation by default
        // This could be extended in the future if needed
        return false;
    }

    private static IEnumerable<object> GetComplexVariables(System.Type propertyType)
    {
        // Find the concrete implementation type that implements ISailfishComplexVariableProvider<T>
        var complexInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(Contracts.Public.Variables.ISailfishComplexVariableProvider<>));

        if (complexInterface == null)
        {
            throw new System.Exception($"Type {propertyType} does not implement ISailfishComplexVariableProvider<T>.");
        }

        // Get the generic argument (the concrete type T)
        var concreteType = complexInterface.GetGenericArguments()[0];

        // Find the GetVariableInstances method on the concrete type
        var method = concreteType.GetMethod("GetVariableInstances",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new System.Exception($"Could not find GetVariableInstances() method on type {concreteType}.");
        }

        var result = method.Invoke(null, null);
        if (result is not System.Collections.IEnumerable variables)
        {
            throw new System.Exception($"GetVariableInstances() method on type {concreteType} did not return IEnumerable.");
        }

        return variables.Cast<object>();
    }
}
