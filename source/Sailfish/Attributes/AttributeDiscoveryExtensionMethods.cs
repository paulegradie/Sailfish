using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Attributes;

internal static class AttributeDiscoveryExtensionMethods
{
    internal static bool PropertyDoesNotHaveAnySailfishVariableAttributes(this PropertyInfo propertyInfo)
    {
        return !propertyInfo.GetCustomAttributes<SailfishVariableAttribute>().Any() && !propertyInfo.GetCustomAttributes<SailfishRangeVariableAttribute>().Any();
    }

    internal static List<PropertyInfo> CollectAllSailfishVariableAttributes(this Type type)
    {
        return type.GetPropertiesWithAttribute<SailfishVariableAttribute>().Concat(type.GetPropertiesWithAttribute<SailfishRangeVariableAttribute>()).ToList();
    }

    internal static bool IsSailfishVariableAttribute(this Attribute attribute)
    {
        return attribute is SailfishVariableAttribute or SailfishRangeVariableAttribute;
    }


    internal static ISailfishVariableAttribute GetSailfishVariableAttributeOrThrow(this PropertyInfo propertyInfo)
    {
        var attribute = propertyInfo
            .GetCustomAttributes<SailfishVariableAttribute>()
            .Union<ISailfishVariableAttribute>(
                propertyInfo.GetCustomAttributes<SailfishRangeVariableAttribute>())
            .SingleOrDefault();
        if (attribute is null) throw new SailfishException($"Multiple ISailfishVariable attributes found on {propertyInfo.Name}");
        return attribute;
    }

    internal static bool IsSailfishComplexityVariable(this PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttributes<SailfishVariableAttribute>().Any(a => a.IsScaleFishVariable())
               || propertyInfo.GetCustomAttributes<SailfishRangeVariableAttribute>().Any(a => a.IsScaleFishVariable());
    }

    /// <summary>
    /// Collects all properties that implement ISailfishVariables interface
    /// </summary>
    internal static List<PropertyInfo> CollectAllSailfishVariablesProperties(this Type type)
    {
        return type.GetProperties()
            .Where(prop => prop.PropertyType.ImplementsISailfishVariables())
            .ToList();
    }

    /// <summary>
    /// Collects all properties that are of type SailfishVariables&lt;T, TProvider&gt;
    /// </summary>
    internal static List<PropertyInfo> CollectAllSailfishVariablesClassProperties(this Type type)
    {
        return type.GetProperties()
            .Where(prop => prop.PropertyType.IsSailfishVariablesClass())
            .ToList();
    }

    /// <summary>
    /// Collects all variable properties: attribute-based, interface-based, and class-based
    /// </summary>
    internal static List<PropertyInfo> CollectAllVariableProperties(this Type type)
    {
        var attributeProperties = type.CollectAllSailfishVariableAttributes();
        var variablesInterfaceProperties = type.CollectAllSailfishVariablesProperties();
        var variablesClassProperties = type.CollectAllSailfishVariablesClassProperties();
        return attributeProperties
            .Concat(variablesInterfaceProperties)
            .Concat(variablesClassProperties)
            .ToList();
    }

    /// <summary>
    /// Checks if a type implements ISailfishVariables interface
    /// </summary>
    internal static bool ImplementsISailfishVariables(this Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISailfishVariables<,>));
    }

    /// <summary>
    /// Checks if a property type implements ISailfishVariables interface
    /// </summary>
    internal static bool IsVariablesProperty(this PropertyInfo propertyInfo)
    {
        return propertyInfo.PropertyType.ImplementsISailfishVariables();
    }

    /// <summary>
    /// Checks if a type is SailfishVariables&lt;T, TProvider&gt;
    /// </summary>
    internal static bool IsSailfishVariablesClass(this Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(SailfishVariables<,>);
    }

    /// <summary>
    /// Checks if a property type is SailfishVariables&lt;T, TProvider&gt;
    /// </summary>
    internal static bool IsSailfishVariablesClassProperty(this PropertyInfo propertyInfo)
    {
        return propertyInfo.PropertyType.IsSailfishVariablesClass();
    }

    /// <summary>
    /// Checks if a property has any Sailfish variable attributes (attribute-based or interface-based)
    /// </summary>
    internal static bool HasAnySailfishVariableConfiguration(this PropertyInfo propertyInfo)
    {
        return !propertyInfo.PropertyDoesNotHaveAnySailfishVariableAttributes() ||
               propertyInfo.IsVariablesProperty() ||
               propertyInfo.IsComplexVariableProperty();
    }

    /// <summary>
    /// Collects all properties that implement ISailfishComplexVariableProvider interface
    /// </summary>
    internal static List<PropertyInfo> CollectAllComplexVariableProperties(this Type type)
    {
        return type.GetProperties()
            .Where(prop => prop.PropertyType.ImplementsISailfishComplexVariableProvider())
            .ToList();
    }

    /// <summary>
    /// Checks if a type implements ISailfishComplexVariableProvider interface
    /// </summary>
    internal static bool ImplementsISailfishComplexVariableProvider(this Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISailfishComplexVariableProvider<>));
    }

    /// <summary>
    /// Checks if a property type implements ISailfishComplexVariableProvider interface
    /// </summary>
    internal static bool IsComplexVariableProperty(this PropertyInfo propertyInfo)
    {
        return propertyInfo.PropertyType.ImplementsISailfishComplexVariableProvider();
    }
}