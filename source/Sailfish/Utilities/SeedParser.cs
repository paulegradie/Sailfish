using System;
using Sailfish.Extensions.Types;

namespace Sailfish.Utilities;

public static class SeedParser
{
    /// <summary>
    /// Parse a deterministic randomization seed from args.
    /// Recognized keys (case-insensitive): "seed", "randomseed", "rng".
    /// Values must be parseable as int. Swallows exceptions and returns null on failure.
    /// </summary>
    public static int? TryParseSeed(OrderedDictionary args)
    {
        try
        {
            foreach (var kv in args)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (string.Equals(key, "seed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "randomseed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "rng", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var s)) return s;
                }
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }
}

