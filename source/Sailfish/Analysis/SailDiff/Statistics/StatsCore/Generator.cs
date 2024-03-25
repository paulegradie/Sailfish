using System;
using System.Threading;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public static class Generator
{
    private static readonly Random SourceRandom = new();
    private static readonly object SourceRandomLock = new();
    public static readonly int? SourceSeed;
    private static readonly int SourceLastUpdateTicks;
    private static readonly object SourceSeedLock = new();
    [ThreadStatic] private static int _threadLastUpdateTicks;
    [ThreadStatic] private static bool _threadOverriden;
    [ThreadStatic] private static int? _threadSeed;
    [ThreadStatic] private static Random _threadRandom;

    public static bool HasBeenAccessed { get; set; }

    public static Random Random
    {
        get
        {
            HasBeenAccessed = true;
            if (_threadOverriden || (_threadRandom != null && _threadLastUpdateTicks >= SourceLastUpdateTicks))
                return _threadRandom;
            _threadSeed = GetRandomSeed();
            _threadLastUpdateTicks = SourceLastUpdateTicks;
            _threadRandom = _threadSeed.HasValue ? new Random(_threadSeed.Value) : new Random();
            return _threadRandom;
        }
    }

    private static int GetRandomSeed()
    {
        lock (SourceRandomLock)
        {
            lock (SourceSeedLock)
            {
                if (SourceRandom != null)
                    return SourceRandom.Next();
                if (!SourceSeed.HasValue)
                    return (int)((13 * Thread.CurrentThread.ManagedThreadId) ^ DateTime.Now.Ticks);
                return SourceSeed.Value > 0 ? (13 * Thread.CurrentThread.ManagedThreadId) ^ SourceSeed.Value : SourceSeed.Value;
            }
        }
    }
}