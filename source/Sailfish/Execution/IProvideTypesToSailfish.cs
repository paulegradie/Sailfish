using System;

namespace Sailfish.Execution;

public abstract class SailfishTypeProvider : ITypeResolver
{
    protected SailfishTypeProvider()
    {
    }

    public abstract object ResolveType(Type type);
    public abstract T ResolveType<T>();
}