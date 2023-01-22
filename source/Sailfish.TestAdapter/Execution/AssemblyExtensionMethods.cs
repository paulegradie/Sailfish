using System;
using System.Linq;
using System.Reflection;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.Execution;

public static class AssemblyExtensionMethods
{
    public static ITypeResolver? GetTypeResolverOrNull(this Assembly assembly)
    {
        var type = assembly.GetTypes().SingleOrDefault(x => x.BaseType == typeof(SailfishTypeProvider));

        if (type is not null)
        {
            return Activator.CreateInstance(type) as ITypeResolver;
        }

        return null;
    }
}