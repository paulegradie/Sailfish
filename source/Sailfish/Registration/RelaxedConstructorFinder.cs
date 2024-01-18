using Autofac.Core.Activators.Reflection;
using System;
using System.Reflection;

namespace Sailfish.Registration;

internal class RelaxedConstructorFinder : IConstructorFinder
{
    public ConstructorInfo[] FindConstructors(Type targetType)
    {
        var constructors = targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return [.. constructors];
    }
}