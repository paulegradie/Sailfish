using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Exceptions;

namespace Sailfish.ExtensionMethods;

public static class ReflectionExtensionMethods
{
    internal static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false) where TAttribute : Attribute
    {
        return method.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static bool HasAttribute<TAttribute>(this PropertyInfo property, bool inherit = false) where TAttribute : Attribute
    {
        return property.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
    }

    internal static bool IsMethodWithAttribute<TAttribute>(this MethodInfo method) where TAttribute : Attribute
    {
        return method.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
    }

    internal static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type.GetProperties().Where(x => x.HasAttribute<TAttribute>());
    }

    internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type.GetMethods().Where(x => x.IsPublic).Where(x => x.HasAttribute<TAttribute>());
    }

    internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this object instance) where TAttribute : Attribute
    {
        return instance.GetType().GetMethodsWithAttribute<TAttribute>();
    }

    internal static MethodInfo? GetMethodWithAttribute<TAttribute>(this object instance) where TAttribute : Attribute
    {
        return instance.GetType().GetMethodsWithAttribute<TAttribute>().SingleOrDefault();
    }

    internal static bool IsAsyncMethod(this MethodInfo method)
    {
        return method.HasAttribute<AsyncStateMachineAttribute>();
    }

    public static async Task InvokeAs(this MethodInfo method, object instance)
    {
        if (method.IsAsyncMethod())
        {
            await (Task)method.Invoke(instance, null)!;
        }
        else method.Invoke(instance, null);
    }

    public static async Task InvokeAsWithCancellation(this MethodInfo method, object instance, CancellationToken cancellationToken)
    {
        var parameters = new object[] { cancellationToken };
        if (method.IsAsyncMethod()) await (Task)method.Invoke(instance, parameters)!;
        else method.Invoke(instance, parameters);
    }

    public static async Task TryInvoke(this MethodInfo? methodInfo, object instance, CancellationToken cancellationToken)
    {
        if (methodInfo is null) return;

        var parameters = methodInfo.GetParameters().ToList();
        switch (parameters.Count)
        {
            case 0:
                await methodInfo.InvokeAs(instance);
                break;
            case 1:
                var paramIsCancellationToken = parameters.SingleOrDefault()?.ParameterType == typeof(CancellationToken);
                if (!paramIsCancellationToken) throw new TestFormatException("Parameter injection is only supported for the CancellationToken type");
                await methodInfo.InvokeAsWithCancellation(instance, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new TestFormatException("Parameter injection is only supported for the CancellationToken type");
        }
    }

    internal static int GetNumIterations(this Type type)
    {
        var numIterations = type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .NumIterations;
        return numIterations;
    }

    internal static int GetWarmupIterations(this Type type)
    {
        var numWarmupIterations = type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .NumWarmupIterations;
        return numWarmupIterations;
    }

    internal static bool SailfishTypeIsDisabled(this Type type)
    {
        var disabled = type
            .GetCustomAttributes(true)
            .OfType<SailfishAttribute>()
            .Single()
            .Disabled;
        return disabled;
    }
}