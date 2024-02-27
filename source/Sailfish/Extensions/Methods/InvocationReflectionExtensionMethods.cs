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

internal static class InvocationReflectionExtensionMethods 
{

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
        switch (parameters.Count)
        {
            case > 1:
                throw new TestFormatException(errorMsg);
            case 1:
                {
                    var paramIsCancellationToken = parameters.Single().ParameterType == typeof(CancellationToken);
                    if (!paramIsCancellationToken)
                    {
                        throw new TestFormatException(errorMsg);
                    }
                    arguments.Add(cancellationToken);
                    break;
                }
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

}
