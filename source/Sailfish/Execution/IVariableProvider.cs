using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;

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
/// Variable provider for interface-based variables (ISailfishVariables&lt;TType, TTypeProvider&gt;)
/// </summary>
internal class TypedVariableProvider : IVariableProvider
{
    private readonly System.Type propertyType;

    public TypedVariableProvider(System.Type propertyType)
    {
        this.propertyType = propertyType;
    }

    public IEnumerable<object> GetVariables()
    {
        return GetTypedVariables(propertyType);
    }

    public bool IsScaleFishVariable()
    {
        // Typed variables are not used for complexity estimation by default
        // This could be extended in the future if needed
        return false;
    }

    private static IEnumerable<object> GetTypedVariables(System.Type propertyType)
    {
        // Find the ISailfishVariables<TType, TTypeProvider> interface
        var variablesInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(ISailfishVariables<,>));

        if (variablesInterface == null)
        {
            throw new System.Exception($"Type {propertyType} does not implement ISailfishVariables<TType, TTypeProvider>.");
        }

        // Get the generic arguments (TType and TTypeProvider)
        var genericArgs = variablesInterface.GetGenericArguments();
        var dataType = genericArgs[0];
        var providerType = genericArgs[1];

        // Create an instance of the provider
        var providerInstance = System.Activator.CreateInstance(providerType);
        if (providerInstance == null)
        {
            throw new System.Exception($"Could not create instance of provider type {providerType}.");
        }

        // Find the Variables() method on the provider
        var method = providerType.GetMethod("Variables");
        if (method == null)
        {
            throw new System.Exception($"Could not find Variables() method on provider type {providerType}.");
        }

        // Invoke the method to get the variables
        var result = method.Invoke(providerInstance, null);
        if (result is not System.Collections.IEnumerable enumerable)
        {
            throw new System.Exception($"Variables() method on {providerType} did not return an IEnumerable.");
        }

        // Convert to object enumerable
        return enumerable.Cast<object>();
    }
}

/// <summary>
/// Variable provider for complex variable providers (ISailfishComplexVariableProvider&lt;T&gt;)
/// This is for backward compatibility with the older approach
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
        return false;
    }

    private static IEnumerable<object> GetComplexVariables(System.Type propertyType)
    {
        // Find the ISailfishComplexVariableProvider<T> interface
        var complexInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(ISailfishComplexVariableProvider<>));

        if (complexInterface == null)
        {
            throw new System.Exception($"Type {propertyType} does not implement ISailfishComplexVariableProvider<T>.");
        }

        // Get the concrete type that implements the interface
        var concreteType = complexInterface.GetGenericArguments()[0];

        // Find the static GetVariableInstances method
        var method = concreteType.GetMethod("GetVariableInstances", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        
        if (method == null)
        {
            throw new System.Exception($"Could not find static GetVariableInstances() method on type {concreteType}.");
        }

        // Invoke the static method to get the variables
        var result = method.Invoke(null, null);
        if (result is not System.Collections.IEnumerable enumerable)
        {
            throw new System.Exception($"GetVariableInstances() method on {concreteType} did not return an IEnumerable.");
        }

        // Convert to object enumerable
        return enumerable.Cast<object>();
    }
}
