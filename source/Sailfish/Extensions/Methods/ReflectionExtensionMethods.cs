using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Extensions.Methods;

public static class ReflectionExtensionMethods
{
    internal static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
    { return type.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0; }

    internal static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
        where TAttribute : Attribute
    { return method.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0; }

    private static bool HasAttribute<TAttribute>(this PropertyInfo property, bool inherit = false)
        where TAttribute : Attribute
    { return property.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0; }

    internal static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    { return type.GetProperties().Where(x => x.HasAttribute<TAttribute>()); }

    internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    { return type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => x.HasAttribute<TAttribute>()); }

    internal static bool IsAsyncMethod(this MethodInfo method)
    { return method.HasAttribute<AsyncStateMachineAttribute>(); }

    internal static bool ReturnTypeIsTask(Type returnType)
    {
        return returnType == typeof(Task) ||
            returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    internal static bool ReturnTypeIsValueTask(Type returnType)
    {
        return returnType == typeof(ValueTask) ||
            returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);
    }

    internal static async Task TryInvoke(this MethodInfo? method, object instance, CancellationToken cancellationToken, PerformanceTimer? performanceTimer = null)
    {
        if (method is null) return;
        var parameters = method.GetParameters().ToList();
        var arguments = new List<object> { };
        var errorMsg = $"The '{method.Name}' method in class '{instance.GetType().Name}' may only receive a single '{nameof(CancellationToken)}' parameter";
        if (parameters.Count > 1)
        {
            throw new TestFormatException(errorMsg);
        }
        if (parameters.Count == 1)
        {
            var paramIsCancellationToken = parameters.Single().ParameterType == typeof(CancellationToken);
            if (!paramIsCancellationToken)
            {
                throw new TestFormatException(errorMsg);
            }
            arguments.Add(cancellationToken);
        }

        if (method.IsAsyncMethod())
        {
            if (ReturnTypeIsTask(method.ReturnType))
            {
                performanceTimer?.StartSailfishMethodExecutionTimer();
                await (Task)method.Invoke(instance, [.. arguments])!;
                performanceTimer?.StopSailfishMethodExecutionTimer();
            }
            else if (ReturnTypeIsValueTask(method.ReturnType))
            {
                performanceTimer?.StartSailfishMethodExecutionTimer();
                await (ValueTask)method.Invoke(instance, [.. arguments])!;
                performanceTimer?.StopSailfishMethodExecutionTimer();
            }
            else
            {
                throw new TestFormatException($"The async '{method.Name}' method in class '{instance.GetType().Name}' may only return '{nameof(Task)}' or '{nameof(ValueTask)}'");
            }
        }
        else
        {
            performanceTimer?.StartSailfishMethodExecutionTimer();
            method.Invoke(instance, [.. arguments]);
            performanceTimer?.StopSailfishMethodExecutionTimer();
        }
    }

    internal static int GetSampleSize(this Type type)
    {
        return type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .SampleSize;
    }

    internal static int GetWarmupIterations(this Type type)
    {
        return type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .NumWarmupIterations;
    }

    internal static bool SailfishTypeIsDisabled(this Type type)
    {
        return type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .Disabled;
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