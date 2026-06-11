using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
///
/// <para>
/// The catalog is process-global. Code that needs to mutate it temporarily — above all tests, which
/// may run in parallel with other tests reading the catalog — should do so inside
/// <see cref="BeginIsolatedScope"/>, which confines every registry operation on the current async
/// flow to a private copy.
/// </para>
/// </summary>
public static class ComplexityFunctionRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<Entry> GlobalEntries = new();

    /// <summary>
    /// When set on the current async flow, all registry operations target this list instead of
    /// <see cref="GlobalEntries"/>. See <see cref="BeginIsolatedScope"/>.
    /// </summary>
    private static readonly AsyncLocal<List<Entry>?> ScopedEntries = new();

    private static List<Entry> CurrentEntries => ScopedEntries.Value ?? GlobalEntries;

    /// <summary>True when the current async flow is inside <see cref="BeginIsolatedScope"/>. Diagnostic.</summary>
    internal static bool IsScopeActive => ScopedEntries.Value is not null;

    static ComplexityFunctionRegistry()
    {
        RegisterBuiltIns();
    }

    /// <summary>
    /// Adds a complexity family to the catalog. Re-registering an already-known name replaces the
    /// previous entry (useful for tests; harmless for one-shot setup).
    ///
    /// <para>
    /// The registration key is the runtime instance's <see cref="ScaleFishModelFunction.Name"/>, which is
    /// what <see cref="ComplexityFunctionConverter"/> writes into JSON and reads back during deserialization.
    /// This means custom families whose display name differs from their CLR type name still round-trip
    /// correctly (e.g. a class <c>LogLog</c> can choose to serialise as <c>"MyLogLog"</c>).
    /// </para>
    /// </summary>
    public static void Register<T>() where T : ScaleFishModelFunction, new()
    {
        Register<T>(includeInFitting: true);
    }

    /// <summary>
    /// Adds a complexity family to the catalog, optionally excluding it from the estimator's candidate
    /// set. A family registered with <paramref name="includeInFitting"/> = false can still be
    /// deserialized from persisted model files (and used for predictions), but is never fitted against
    /// new measurements. The built-in <see cref="ComplexityFunctions.LogLinear"/> family uses this mode
    /// because its basis is collinear with <see cref="ComplexityFunctions.NLogN"/> — fitting both would
    /// make every n·log n classification appear statistically indistinguishable from itself.
    /// </summary>
    public static void Register<T>(bool includeInFitting) where T : ScaleFishModelFunction, new()
    {
        // Use the runtime Name property — that's the string the JSON writer emits and the loader
        // looks up. Using typeof(T).Name would break round-trip for any family whose Name was
        // overridden to something other than the C# type name.
        var name = new T().Name;
        var entry = new Entry(name, () => new T(), element => element.Deserialize<T>(), includeInFitting);
        lock (SyncRoot)
        {
            var entries = CurrentEntries;
            entries.RemoveAll(e => e.Name == name);
            entries.Add(entry);
        }
    }

    /// <summary>
    /// Removes a registered family by name. Returns true if the family was found and removed.
    /// </summary>
    public static bool Unregister(string name)
    {
        lock (SyncRoot)
        {
            return CurrentEntries.RemoveAll(e => e.Name == name) > 0;
        }
    }

    /// <summary>
    /// Returns true when a family with the given name is currently registered.
    /// </summary>
    public static bool IsRegistered(string name)
    {
        lock (SyncRoot)
        {
            return CurrentEntries.Any(e => e.Name == name);
        }
    }

    /// <summary>
    /// Returns fresh instances of every registered family that participates in fitting. The estimator
    /// calls this each fit, so each candidate gets its own mutable
    /// <see cref="ScaleFishModelFunction.FunctionParameters"/> — no shared state across fits or threads.
    /// Families registered as deserialization-only (see <see cref="Register{T}(bool)"/>) are excluded.
    /// </summary>
    public static IReadOnlyList<ScaleFishModelFunction> CreateFitInstances()
    {
        lock (SyncRoot)
        {
            return CurrentEntries.Where(e => e.IncludeInFitting).Select(e => e.Factory()).ToList();
        }
    }

    /// <summary>
    /// JSON loader hook: reconstructs the named family from the given element. Returns null when no
    /// matching family is registered (the caller decides whether to throw, skip, or substitute).
    /// </summary>
    public static ScaleFishModelFunction? Deserialize(string name, JsonElement element)
    {
        Entry? entry;
        lock (SyncRoot)
        {
            entry = CurrentEntries.FirstOrDefault(e => e.Name == name);
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
            return CurrentEntries.Select(e => e.Name).ToList();
        }
    }

    /// <summary>
    /// Removes any custom registrations and restores the built-in catalog to its default state. Applies
    /// to the catalog the current async flow sees: inside <see cref="BeginIsolatedScope"/> it resets the
    /// scope's private copy; otherwise the process-global catalog. Prefer an isolated scope for test
    /// cleanup — resetting the global catalog from a test races every concurrently-running test that
    /// reads it.
    /// </summary>
    public static void ResetToBuiltIns()
    {
        lock (SyncRoot)
        {
            CurrentEntries.Clear();
        }
        RegisterBuiltIns();
    }

    /// <summary>
    /// Begins an isolated registry scope on the current async flow: the catalog is copied, and every
    /// registry operation performed while the scope is active — registrations, unregistrations, resets,
    /// and reads — applies to that private copy. Other threads and async flows (e.g. other tests running
    /// in parallel) continue to see the unmodified catalog; disposing restores whatever the flow saw
    /// before. Scopes nest.
    ///
    /// <para>
    /// This exists because the catalog is process-global mutable state: a test that registers a custom
    /// family while another test concurrently fits measurements would otherwise race — the second test's
    /// candidate set silently changes mid-run. Wrap any temporary registration in a scope:
    /// <code>
    /// using var scope = ComplexityFunctionRegistry.BeginIsolatedScope();
    /// ComplexityFunctionRegistry.Register&lt;MyFamily&gt;();
    /// // ... exercise the estimator — only this async flow sees MyFamily ...
    /// </code>
    /// </para>
    /// </summary>
    public static IDisposable BeginIsolatedScope()
    {
        lock (SyncRoot)
        {
            var previous = ScopedEntries.Value;
            ScopedEntries.Value = new List<Entry>(CurrentEntries);
            return new IsolatedScope(previous);
        }
    }

    private static void RegisterBuiltIns()
    {
        Register<Constant>();
        Register<Logarithmic>();
        Register<SqrtN>();
        Register<Linear>();
        Register<NLogN>();
        Register<Quadratic>();
        Register<Cubic>();
        // LogLinear is x·log₂(x) — a constant multiple of NLogN's x·ln(x) basis, so both produce
        // identical fits. Keeping it in the candidate set made the top two candidates tie whenever
        // the truth was n·log n (Δ-AICc ≈ 0 ⇒ IsDistinguishable always false, Akaike weight halved,
        // bootstrap selection-agreement split ~50/50). Registered deserialization-only so persisted
        // models that classified as LogLinear keep loading and predicting.
        Register<LogLinear>(includeInFitting: false);
        Register<Exponential>();
        Register<Factorial>();
    }

    private sealed class IsolatedScope : IDisposable
    {
        private readonly List<Entry>? _previous;
        private bool _disposed;

        public IsolatedScope(List<Entry>? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ScopedEntries.Value = _previous;
        }
    }

    private sealed record Entry(
        string Name,
        Func<ScaleFishModelFunction> Factory,
        Func<JsonElement, ScaleFishModelFunction?> Deserializer,
        bool IncludeInFitting);
}
