using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using System;
using System.Linq;
using System.Reflection;

namespace Sailfish.TestAdapter.Discovery;

internal static class TypeLoader
{
    public static Type[] LoadSailfishTestTypesFrom(string sourceDll, IMessageLogger logger)
    {
        var assembly = LoadAssemblyFromDll(sourceDll);
        var types = CollectSailfishTestTypesFromAssembly(assembly, logger);
        return types;
    }

    private static Type[] CollectSailfishTestTypesFromAssembly(Assembly assembly, IMessageLogger logger)
    {
        var perfTestTypes = assembly
            .GetTypes()
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .ToArray();

#if debug
        foreach (var testType in perfTestTypes)
        {
            if (logger is null)
            {
                Console.WriteLine($" - Perf tests: {testType.Name}");
            }
            else
            {
                logger?.SendMessage(TestMessageLevel.Informational, $" - Perf tests: {testType.Name}");
            }
        }
#endif
        return perfTestTypes;
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }
}