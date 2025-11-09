using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sailfish.Utilities
{
    /// <summary>
    /// Anti-DCE consumer utility. Call <see cref="Consume{T}(T)"/> with values computed in the hot path
    /// to discourage the JIT from eliminating work as dead code during benchmarking.
    /// Inspired by BenchmarkDotNet's consumer pattern.
    /// </summary>
    public static class Consumer
    {
        // Single blackhole reference. Using object avoids generic static duplication and works for value types via boxing.
        private static object? _blackhole;

        /// <summary>
        /// Consumes a value with observable side effects that are cheap and hard to optimize away.
        /// - Volatile.Write introduces a memory barrier and a write to a static location.
        /// - GC.KeepAlive prevents the value (or any graph it references) from being optimized as dead.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Consume<T>(T value)
        {
            Volatile.Write(ref _blackhole, value);
            GC.KeepAlive(_blackhole);
        }
    }
}

