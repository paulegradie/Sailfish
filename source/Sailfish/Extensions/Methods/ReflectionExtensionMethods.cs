using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sailfish.Extensions.Methods;

public static class ReflectionExtensionMethods
{
    internal static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
        where TAttribute : Attribute
    {
        return method.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    private static bool HasAttribute<TAttribute>(this PropertyInfo property, bool inherit = false)
        where TAttribute : Attribute
    {
        return property.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        return type.GetProperties().Where(x => x.HasAttribute<TAttribute>());
    }

    internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => x.HasAttribute<TAttribute>());
    }

    public static List<MethodInfo> FindMethodsDecoratedWithAttribute<TAttribute>(this object obj)
        where TAttribute : Attribute
    {
        var type = obj.GetType();

        var methods = type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes(typeof(TAttribute), true).Length > 0)
            .OrderBy(m => m.Name)
            .ToArray();

        var baseType = type.BaseType;
        while (baseType != null)
        {
            var baseMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttributes(typeof(TAttribute), true).Length > 0)
                .OrderBy(m => m.Name)
                .ToArray();

            methods = [.. baseMethods, .. methods];
            baseType = baseType.BaseType;
        }

        return [.. methods];
    }
}