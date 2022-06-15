using System;

namespace VeerPerforma.Execution
{
    public interface ITypeResolver
    {
        object ResolveType(Type type);
    }
}