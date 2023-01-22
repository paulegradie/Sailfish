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

    private Type[] AllTypesInDll { get; set; }

    public object ResolveType(Type type)
    {
        return AllTypesInDll.Single(x => x == type);
    }

    public T ResolveType<T>()
    {
        return (T)ResolveType(typeof(T));
    }

    private static Type[] CollectSailfishTestTypesFromAssembly(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?

        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .ToArray();

        return perfTestTypes;
    }
}