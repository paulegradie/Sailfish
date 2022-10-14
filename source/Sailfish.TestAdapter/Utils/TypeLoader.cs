using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Utils;

internal class TypeLoader
{
    public Type[] LoadTypes(IEnumerable<string> sourceDlls)
    {
        return sourceDlls.SelectMany(dll => LoadTypes(dll)).ToArray();
    }

    public Type[] LoadTypes(string sourceDll)
    {
        var assembly = LoadAssemblyFromDll(sourceDll);
        var types = CollectTestTypesFromAssembly(assembly);
        return types;
    }

    private Type[] CollectTestTypesFromAssembly(Assembly assembly)
    {
        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .ToArray();

        logger.Verbose("\rTest Types Discovered in {Assembly}:\r", assembly.FullName ?? "Couldn't Find the assembly name property");
        foreach (var testType in perfTestTypes) logger.Verbose("--- Perf tests: {0}", testType.Name);

        return perfTestTypes;
    }

    private Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }
}