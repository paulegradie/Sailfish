using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using VeerPerforma.Statistics;

namespace VeerPerforma.Execution
{
    public class TestInstanceContainerCreator : ITestInstanceContainerCreator
    {
        private readonly IMethodOrganizer methodOrganizer;
        private readonly IParameterGridCreator parameterGridCreator;
        private readonly ITypeResolver typeResolver;

        public TestInstanceContainerCreator(
            ITypeResolver typeResolver,
            IParameterGridCreator parameterGridCreator,
            IMethodOrganizer methodOrganizer)
        {
            this.typeResolver = typeResolver;
            this.parameterGridCreator = parameterGridCreator;
            this.methodOrganizer = methodOrganizer;
        }

        public List<TestInstanceContainer> CreateTestContainerInstances(Type test)
        {
            var (propNames, combos) = parameterGridCreator.GenerateParameterGrid(test);
            var instances = CreateInstances(test, combos, propNames);
            var methodInstancePairs = methodOrganizer.FormMethodGroups(instances);

            var executionSettings = test.RetrieveExecutionTestSettings();

            var testContainers = new List<TestInstanceContainer>();
            foreach (var pair in methodInstancePairs.Values)
            {
                if (pair.Count != combos.Length) throw new Exception("Instances and combos for some reason did not match");
                foreach (var ((method, instance), variableCombo) in pair.Zip(combos))
                {
                    var containers = TestInstanceContainer.CreateTestInstance(instance, method, propNames.ToArray(), variableCombo, executionSettings);
                    testContainers.Add(containers);
                }
            }

            return testContainers.OrderBy(x => x.ExecutionMethod.Name).ToList();
        }

        private List<object> CreateInstances(Type test, IEnumerable<IEnumerable<int>> combos, List<string> propNames)
        {
            var instances = new List<object>();
            var ctorArgTypes = GetCtorParamTypes(test);

            foreach (var combo in combos)
            {
                var instance = CreateTestInstance(test, ctorArgTypes);

                foreach (var (propertyName, variableValue) in propNames.Zip(combo))
                {
                    var prop = instance.GetType().GetProperties().Single(x => x.Name == propertyName);
                    prop.SetValue(instance, variableValue);
                }

                instances.Add(instance);
            }

            return instances;
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
    }

    public static class ExecutionExtensionMethods
    {
        public static ExecutionSettings RetrieveExecutionTestSettings(this Type type)
        {
            return new ExecutionSettings()
            {
                AsCsv = false
            };
        }
    }
}