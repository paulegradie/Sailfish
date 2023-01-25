using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;

namespace Sailfish.Execution;

public static class AssemblyScannerCache
{
    private static readonly MemoryCache Cache = new("AssemblyScanCache");
    private const string CacheKey = "AllTypes";

    /// <summary>
    /// Note - if we take types from multiple assemblies, this might fails to fine the new types
    /// So this could be the source of a painful bug
    /// </summary>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetTypesInAssemblies(IEnumerable<Assembly> assemblies)
    {
        if (Cache.Get(CacheKey) is IEnumerable<Type> cachedResult)
        {
            var cached = cachedResult.ToList();
            if (cached.Count > 0)
            {
                return cached;
            }
        }

        // If not found in the cache, scan the assembly
        var allAssemblyTypes = assemblies.Distinct().SelectMany(a => a.GetTypes());

        // Create a cache policy
        var policy = new CacheItemPolicy();

        // Add the result to the cache
        Cache.Add(CacheKey, allAssemblyTypes, policy);

        return allAssemblyTypes;
    }
}