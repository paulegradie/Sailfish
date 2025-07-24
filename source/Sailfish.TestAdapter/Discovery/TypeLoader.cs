using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;

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
        Type[] allTypes;

        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // When ReflectionTypeLoadException occurs, we can still get the types that loaded successfully
            allTypes = ex.Types.Where(t => t != null).ToArray();

            logger?.SendMessage(TestMessageLevel.Warning,
                $"Some types could not be loaded from assembly {assembly.GetName().Name}. " +
                $"Successfully loaded {allTypes.Length} types. " +
                $"First loader exception: {ex.LoaderExceptions?.FirstOrDefault()?.Message}");
        }

        var perfTestTypes = allTypes
            .Where(x => x.HasAttribute<SailfishAttribute>())
            .Where(x => x.GetCustomAttribute<SailfishAttribute>()?.Disabled != true)
            .ToArray();

        return perfTestTypes;
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }
}