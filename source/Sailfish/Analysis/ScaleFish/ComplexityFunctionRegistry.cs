using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;

namespace Sailfish.Analysis.ScaleFish;

/// <summary>
/// Catalog of complexity families considered by the estimator and recognised by the JSON loader.
/// Built-in families register automatically; users can add custom <see cref="ScaleFishModelFunction"/>
/// subclasses at any time before running Sailfish.
///
/// <para>
/// Example — register a custom family:
/// <code>
/// public class LogLog : ScaleFishModelFunction
/// {
///     public override string Name { get; set; } = nameof(LogLog);
///     public override string OName { get; set; } = "O(log(log(n)))";
///     public override string Quality { get; set; } = "Excellent";
///     public override string FunctionDef { get; set; } = "f(x) = {0}*log(log(x)) + {1}";
///     public override double Compute(double bias, double scale, double x) =&gt; scale * Math.Log(Math.Log(x)) + bias;
/// }
///
/// ComplexityFunctionRegistry.Register&lt;LogLog&gt;();
/// </code>
/// </para>
/// </summary>
public static class ComplexityFunctionRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<Entry> Entries = new();

    static ComplexityFunctionRegistry()
    {
        RegisterBuiltIns();
    }

    /// <summary>
    /// Adds a complexity family to the catalog. Re-registering an already-known name replaces the
    /// previous entry (useful for tests; harmless for one-shot setup).
    /// </summary>
    public static void Register<T>() where T : ScaleFishModelFunction, new()
    {
        var name = typeof(T).Name;
        var entry = new Entry(name, () => new T(), element => element.Deserialize<T>());
        lock (SyncRoot)
        {
            Entries.RemoveAll(e => e.Name == name);
            Entries.Add(entry);
        }
    }

    /// <summary>
    /// Removes a registered family by name. Returns true if the family was found and removed.
    /// </summary>
    public static bool Unregister(string name)
    {
        lock (SyncRoot)
        {
            return Entries.RemoveAll(e => e.Name == name) > 0;
        }
    }

    /// <summary>
    /// Returns true when a family with the given name is currently registered.
    /// </summary>
    public static bool IsRegistered(string name)
    {
        lock (SyncRoot)
        {
            return Entries.Any(e => e.Name == name);
        }
    }

    /// <summary>
    /// Returns fresh instances of every registered family. The estimator calls this each fit, so each
    /// candidate gets its own mutable <see cref="ScaleFishModelFunction.FunctionParameters"/> — no shared
    /// state across fits or threads.
    /// </summary>
    public static IReadOnlyList<ScaleFishModelFunction> CreateFitInstances()
    {
        lock (SyncRoot)
        {
            return Entries.Select(e => e.Factory()).ToList();
        }
    }

    /// <summary>
    /// JSON loader hook: reconstructs the named family from the given element. Returns null when no
    /// matching family is registered (the caller decides whether to throw, skip, or substitute).
    /// </summary>
    public static ScaleFishModelFunction? Deserialize(string name, JsonElement element)
    {
        Entry entry;
        lock (SyncRoot)
        {
            entry = Entries.FirstOrDefault(e => e.Name == name);
        }
        if (entry is null) return null;
        return entry.Deserializer(element);
    }

    /// <summary>
    /// Snapshot of registered family names. Intended for debugging and tests.
    /// </summary>
    public static IReadOnlyList<string> RegisteredNames()
    {
        lock (SyncRoot)
        {
            return Entries.Select(e => e.Name).ToList();
        }
    }

    /// <summary>
    /// Removes any custom registrations and restores the built-in catalog to its default state.
    /// Intended for test cleanup — production code should not call this.
    /// </summary>
    public static void ResetToBuiltIns()
    {
        lock (SyncRoot)
        {
            Entries.Clear();
        }
        RegisterBuiltIns();
    }

    private static void RegisterBuiltIns()
    {
        Register<Linear>();
        Register<NLogN>();
        Register<Quadratic>();
        Register<Cubic>();
        Register<LogLinear>();
        Register<Exponential>();
        Register<Factorial>();
        Register<SqrtN>();
    }

    private sealed record Entry(string Name, Func<ScaleFishModelFunction> Factory, Func<JsonElement, ScaleFishModelFunction?> Deserializer);
}
