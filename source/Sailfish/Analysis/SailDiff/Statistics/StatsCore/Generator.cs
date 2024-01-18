using System;
using System.Threading;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public static class Generator
{
    private static readonly Random sourceRandom = new();
    private static readonly object sourceRandomLock = new();
    private static readonly int? sourceSeed;
    private static readonly int sourceLastUpdateTicks;
    private static readonly object sourceSeedLock = new();
    [ThreadStatic] private static int threadLastUpdateTicks;
    [ThreadStatic] private static bool threadOverriden;
    [ThreadStatic] private static int? threadSeed;
    [ThreadStatic] private static Random threadRandom;

    public static bool HasBeenAccessed { get; set; }

    public static Random Random
    {
        get
        {
            HasBeenAccessed = true;
            if (threadOverriden || (threadRandom != null && threadLastUpdateTicks >= sourceLastUpdateTicks))
                return threadRandom;
            threadSeed = GetRandomSeed();
            threadLastUpdateTicks = sourceLastUpdateTicks;
            threadRandom = threadSeed.HasValue ? new Random(threadSeed.Value) : new Random();
            return threadRandom;
        }
    }

    private static int GetRandomSeed()
    {
        lock (sourceRandomLock)
        {
            lock (sourceSeedLock)
            {
                if (sourceRandom != null)
                    return sourceRandom.Next();
                if (!sourceSeed.HasValue)
                    return (int)((13 * Thread.CurrentThread.ManagedThreadId) ^ DateTime.Now.Ticks);
                return sourceSeed.Value > 0 ? (13 * Thread.CurrentThread.ManagedThreadId) ^ sourceSeed.Value : sourceSeed.Value;
            }
        }
    }
}