using System;

namespace Sailfish.Execution
{
    internal interface ITypeResolver
    {
        object ResolveType(Type type);
    }
}