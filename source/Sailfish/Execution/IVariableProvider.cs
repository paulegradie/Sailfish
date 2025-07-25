using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Exceptions;
using System;
using System.Collections;

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
        this.attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
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
        this.propertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
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
            throw new SailfishException($"Type {propertyType} does not implement ISailfishVariables<TType, TTypeProvider>.");
        }

        // Get the generic arguments (TType and TTypeProvider)
        var genericArgs = variablesInterface.GetGenericArguments();
        var dataType = genericArgs[0];
        var providerType = genericArgs[1];

        // Create an instance of the provider
        var providerInstance = System.Activator.CreateInstance(providerType);
        if (providerInstance == null)
        {
            throw new SailfishException($"Could not create instance of provider type {providerType}. Ensure it has a parameterless constructor.");
        }

        // Find the Variables() method on the provider
        var method = providerType.GetMethod("Variables");
        if (method == null)
        {
            throw new SailfishException($"Could not find Variables() method on provider type {providerType}.");
        }

        // Invoke the method to get the variables
        var result = method.Invoke(providerInstance, null);
        if (result is not System.Collections.IEnumerable enumerable)
        {
            throw new SailfishException($"Variables() method on {providerType} did not return an IEnumerable.");
        }

        // Convert to object enumerable
        return enumerable.Cast<object>();
    }
}

/// <summary>
/// Variable provider for SailfishVariables&lt;T, TProvider&gt; class-based variables
/// </summary>
internal class SailfishVariablesClassProvider : IVariableProvider
{
    private readonly System.Type propertyType;

    public SailfishVariablesClassProvider(System.Type propertyType)
    {
        this.propertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
    }

    public IEnumerable<object> GetVariables()
    {
        return GetSailfishVariablesClassVariables(propertyType);
    }

    public bool IsScaleFishVariable()
    {
        // SailfishVariables class variables are not used for complexity estimation by default
        // This could be extended in the future if needed
        return false;
    }

    private static IEnumerable<object> GetSailfishVariablesClassVariables(System.Type propertyType)
    {
        // Verify this is a SailfishVariables<T, TProvider> type
        if (!propertyType.IsGenericType ||
            propertyType.GetGenericTypeDefinition() != typeof(SailfishVariables<,>))
        {
            throw new SailfishException($"Type {propertyType} is not SailfishVariables<T, TProvider>.");
        }

        // Get the generic arguments (T and TProvider)
        var genericArgs = propertyType.GetGenericArguments();
        var dataType = genericArgs[0];
        var providerType = genericArgs[1];

        // Create an instance of the provider
        var providerInstance = Activator.CreateInstance(providerType);
        if (providerInstance == null)
        {
            throw new SailfishException($"Could not create instance of provider type {providerType}.");
        }

        // Find the Variables() method on the provider
        var method = providerType.GetMethod("Variables");
        if (method == null)
        {
            throw new SailfishException($"Could not find Variables() method on provider type {providerType}.");
        }

        // Invoke the method to get the variables
        var result = method.Invoke(providerInstance, null);
        if (result is not IEnumerable enumerable)
        {
            throw new SailfishException($"Variables() method on {providerType} did not return an IEnumerable.");
        }

        // Convert each variable to a SailfishVariables<T, TProvider> instance
        var sailfishVariablesList = new List<object>();
        foreach (var variable in enumerable)
        {
            // Create a SailfishVariables<T, TProvider> instance and set its Value
            var sailfishVariablesInstance = Activator.CreateInstance(propertyType);
            if (sailfishVariablesInstance == null)
            {
                throw new SailfishException($"Could not create instance of {propertyType}.");
            }

            // Set the Value property
            var valueProperty = propertyType.GetProperty("Value");
            if (valueProperty == null)
            {
                throw new SailfishException($"Could not find Value property on {propertyType}.");
            }

            valueProperty.SetValue(sailfishVariablesInstance, variable);
            sailfishVariablesList.Add(sailfishVariablesInstance);
        }

        return sailfishVariablesList;
    }
}


