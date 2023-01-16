using System;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Utils;

internal class DllTypeResolver : ITypeResolver
{
    public DllTypeResolver(string dllSourceFile)
    {
        AllTypesInDll = CollectSailfishTestTypesFromAssembly(dllSourceFile);
    }

    public Type[] AllTypesInDll { get; set; }

    public object ResolveType(Type type)
    {
        return AllTypesInDll.Single(x => x == type);
    }

    private static Type[] CollectSailfishTestTypesFromAssembly(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?

        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .ToArray();

        CustomLogger.Verbose("\rTest Types Discovered in {Assembly}:\r", assembly.FullName ?? "Couldn't Find the assembly name property");
        foreach (var testType in perfTestTypes) CustomLogger.Verbose("--- Perf tests: {0}", testType.Name);

        return perfTestTypes;
    }
}