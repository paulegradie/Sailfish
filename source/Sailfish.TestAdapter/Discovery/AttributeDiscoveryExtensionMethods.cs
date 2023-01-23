using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sailfish.TestAdapter.Discovery;

internal static class AttributeDiscoveryExtensionMethods
{
    internal static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    private static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false) where TAttribute : Attribute
    {
        return method.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type.GetMethods().Where(x => x.HasAttribute<TAttribute>());
    }
}