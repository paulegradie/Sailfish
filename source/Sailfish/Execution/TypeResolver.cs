using System;
using Autofac;
using Sailfish.Exceptions;

namespace Sailfish.Execution
{
    internal class TypeResolver : ITypeResolver
    {
        private readonly ILifetimeScope lifetimeScope;

        public TypeResolver(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public object ResolveType(Type type)
        {
            var resolvedType = lifetimeScope.Resolve(type);
            if (resolvedType is null)
            {
                var message = $"Please register {type.Name} using the `registerAdditionalTypes` callback in "
                              + $"the main Run method.\r\nSee the Sailfish demo console app for an example.";
                throw new SailfishException(message);
            }

            return resolvedType;
        }
    }
}