using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    internal static bool PropertyHasSailfishVariableAttribute(this PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttributes<SailfishVariableAttribute>().Any() || propertyInfo.GetCustomAttributes<SailfishRangeVariableAttribute>().Any();
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
}