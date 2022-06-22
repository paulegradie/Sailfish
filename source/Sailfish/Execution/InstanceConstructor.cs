using System;

namespace Sailfish.Execution
{
    

public static class InstanceConstructor
{
    // must be parameterless

    public static object CreateInstance(Type type, params int[] args)
    {
        var instance = Activator.CreateInstance(type, args);
        if (instance is null) throw new Exception("Weird program error encountered");
        return instance;
    }
}
}
