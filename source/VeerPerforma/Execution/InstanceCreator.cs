using System.Reflection;
using Autofac;

namespace VeerPerforma.Execution;

public class InstanceCreator : IInstanceCreator
{
    private readonly ITypeResolver typeResolver;

    public InstanceCreator(ITypeResolver typeResolver)
    {
        this.typeResolver = typeResolver;
    }

    private static ConstructorInfo GetConstructor(Type type)
    {
        var ctorInfos = type.GetDeclaredConstructors();
        if (ctorInfos.Length == 0 || ctorInfos.Length > 1) throw new Exception("A single ctor must be declared in all test types");
        return ctorInfos.Single();
    }

    private static Type[] GetCtorParamTypes(Type type)
    {
        var ctorInfo = GetConstructor(type);
        var argTypes = ctorInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        return argTypes;
    }

    private object CreateTestInstance(Type test, Type[] ctorArgTypes)
    {
        var ctorArgs = ctorArgTypes.Select(x => typeResolver.ResolveType(x)).ToArray();
        var instance = Activator.CreateInstance(test, ctorArgs);
        if (instance is null) throw new Exception($"Couldn't create instance of {test.Name}");
        return instance;
    }

    public List<object> CreateInstances(Type test, IEnumerable<IEnumerable<int>> combos, List<string> propNames)
    {
        var instances = new List<object>();
        var ctorArgTypes = GetCtorParamTypes(test);

        foreach (var combo in combos)
        {
            var instance = CreateTestInstance(test, ctorArgTypes);

            foreach (var propMeta in propNames.Zip(combo))
            {
                var prop = instance.GetType().GetProperties().Single(x => x.Name == propMeta.First);
                prop.SetValue(instance, propMeta.Second);
            }

            instances.Add(instance);
        }

        return instances;
    }
}