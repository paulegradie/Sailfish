using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;

namespace Sailfish.TestAdapter.Utils;

internal static class TypeLoader
{
    public static IEnumerable<Type> LoadSailfishTestTypesFrom(IEnumerable<string> sourceDlls)
    {
        return sourceDlls.SelectMany(LoadSailfishTestTypesFrom).ToArray();
    }

    public static Type[] LoadSailfishTestTypesFrom(string sourceDll)
    {
        var assembly = LoadAssemblyFromDll(sourceDll);
        var types = CollectSailfishTestTypesFromAssembly(assembly);
        return types;
    }

    private static Type[] CollectSailfishTestTypesFromAssembly(Assembly assembly)
    {
        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .ToArray();

        CustomLogger.Verbose("\rTest Types Discovered in {Assembly}:\r", assembly.FullName ?? "Couldn't Find the assembly name property");
        foreach (var testType in perfTestTypes) CustomLogger.Verbose("--- Perf tests: {0}", testType.Name);

        return perfTestTypes;
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }
}