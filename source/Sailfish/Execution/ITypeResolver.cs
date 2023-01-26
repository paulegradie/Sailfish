using System;

namespace Sailfish.Execution;

public interface ITypeResolver
{
    object ResolveType(Type type);
    T ResolveType<T>() where T : notnull;
}