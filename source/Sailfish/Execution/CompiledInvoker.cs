using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

/// <summary>
///     Builds a compiled, allocation-free delegate that invokes a Sailfish benchmark method
///     <b>directly</b> — no <see cref="MethodInfo.Invoke" /> per call — normalizing every supported
///     return shape (<c>void</c>, <see cref="Task" />, <c>Task&lt;T&gt;</c>, <see cref="ValueTask" />,
///     <c>ValueTask&lt;T&gt;</c>) to a single <see cref="Func{CancellationToken, ValueTask}" /> shape.
///     <para>
///         Why this matters: reflection dispatch (plus the per-call <c>object[]</c> argument array it
///         requires) adds tens-to-hundreds of nanoseconds of <i>variable</i> overhead to every measured
///         invocation, which sets the noise floor for how small a difference Sailfish can resolve. A
///         compiled delegate is a direct, often-inlinable call with near-zero, low-variance overhead and
///         zero per-call allocation.
///     </para>
///     <para>
///         The single normalized shape also lets the engine subtract a <i>structurally identical</i> idle
///         baseline (<see cref="Empty" />) — the same technique BenchmarkDotNet uses when it subtracts its
///         generated overhead loop — so the subtraction cancels almost exactly instead of approximating.
///     </para>
/// </summary>
internal static class CompiledInvoker
{
    private static readonly ConstructorInfo ValueTaskFromTask =
        typeof(ValueTask).GetConstructor(new[] { typeof(Task) })!;

    /// <summary>
    ///     The idle baseline: identical delegate shape to a compiled workload invoker, empty body,
    ///     completes synchronously. Measuring this through the same loop the workload runs in yields the
    ///     per-invocation harness overhead to subtract.
    /// </summary>
    public static readonly Func<CancellationToken, ValueTask> Empty = static _ => default;

    /// <summary>
    ///     Compiles a direct-call invoker bound to <paramref name="instance" /> for
    ///     <paramref name="method" />. Build once per test-instance and reuse across all warmup, tuning,
    ///     and measured invocations.
    /// </summary>
    public static Func<CancellationToken, ValueTask> Build(object instance, MethodInfo method)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        if (method is null) throw new ArgumentNullException(nameof(method));

        var parameters = method.GetParameters();
        var hasToken = parameters.Length == 1 && parameters[0].ParameterType == typeof(CancellationToken);
        if (parameters.Length > 1 || (parameters.Length == 1 && !hasToken))
        {
            throw new TestFormatException(
                $"The '{method.Name}' method in class '{instance.GetType().Name}' may only receive a single '{nameof(CancellationToken)}' parameter");
        }

        var ct = Expression.Parameter(typeof(CancellationToken), "ct");
        var target = method.IsStatic ? null : Expression.Constant(instance);
        var args = hasToken ? new Expression[] { ct } : Array.Empty<Expression>();
        Expression call = Expression.Call(target, method, args);

        var body = AdaptToValueTask(call, method.ReturnType);
        return Expression.Lambda<Func<CancellationToken, ValueTask>>(body, ct).Compile();
    }

    private static Expression AdaptToValueTask(Expression call, Type returnType)
    {
        // ValueTask -> return as-is (the await happens at the call site, inside the timed region).
        if (returnType == typeof(ValueTask))
            return call;

        // ValueTask<T> -> new ValueTask(call.AsTask()). The result is discarded; rare for benchmarks.
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = returnType.GetMethod(nameof(ValueTask<int>.AsTask), Type.EmptyTypes)!;
            return Expression.New(ValueTaskFromTask, Expression.Call(call, asTask));
        }

        // Task or Task<T> -> new ValueTask((Task)call). Awaiting completes the async work in the timer.
        if (typeof(Task).IsAssignableFrom(returnType))
        {
            var asTask = returnType == typeof(Task) ? call : (Expression)Expression.Convert(call, typeof(Task));
            return Expression.New(ValueTaskFromTask, asTask);
        }

        // void or any synchronous return -> run it, discard any value, hand back a completed ValueTask.
        return Expression.Block(call, Expression.Default(typeof(ValueTask)));
    }
}
