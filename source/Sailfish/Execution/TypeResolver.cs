using System;
using Autofac;
using Sailfish.Exceptions;

namespace Sailfish.Execution
{
    public class TypeResolver : ITypeResolver
    {
        private readonly ILifetimeScope? lifetimeScope;
        private readonly IServiceProvider? serviceProvider;

        public TypeResolver(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public TypeResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object ResolveType(Type type)
        {
            if (lifetimeScope is not null) return lifetimeScope.Resolve(type);

            if (serviceProvider is not null)
            {
                var resolved = serviceProvider.GetService(type);
                if (resolved is null) throw new SailfishException($"Could not resolve test type: {type.Name}. Make sure you register this type in your autofac or service collection container.");
                return resolved;
            }

            throw new SailfishException("Service provider not found from whence to resolve objects");
        }
    }
}