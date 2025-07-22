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
                                i.GetGenericTypeDefinition() == typeof(Contracts.Public.Variables.ISailfishVariables<,>));

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

        var result = method.Invoke(providerInstance, null);
        if (result is not System.Collections.IEnumerable variables)
        {
            throw new System.Exception($"Variables() method on provider type {providerType} did not return IEnumerable.");
        }

        return variables.Cast<object>();
    }
}
