using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Exceptions;

namespace Sailfish.Execution
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

        // TODO: We should make these lazily - if we have a user that
        // provides a server instance to the test runner, we'll potentially instantiate 
        // dozens (or more) of these instances in memory. Gonna have a bad day.
        public List<TestInstanceContainer> CreateTestContainerInstances(Type test)
        {
            var (propNames, combos) = parameterGridCreator.GenerateParameterGrid(test);
            var instances = CreateInstances(test, combos, propNames);
            var methodInstancePairs = methodOrganizer.FormMethodGroups(instances);

            var testContainers = new List<TestInstanceContainer>();
            foreach (var pair in methodInstancePairs.Values)
            {
                if (pair.Count != combos.Length) throw new SailfishException("Instances and combos for some reason did not match");
                foreach (var ((method, instance), variableCombo) in pair.Zip(combos))
                {
                    var containers = TestInstanceContainer.CreateTestInstance(instance, method, propNames.ToArray(), variableCombo);
                    testContainers.Add(containers);
                }
            }

            return testContainers.OrderBy(x => x.ExecutionMethod.Name).ToList();
        }

        private List<object> CreateInstances(Type test, IEnumerable<IEnumerable<int>> combos, List<string> propNames)
        {
            var instances = new List<object>();
            var ctorArgTypes = test.GetCtorParamTypes();

            foreach (var combo in combos)
            {
                // var lazyInstance = CreateLazyInstance(test, ctorArgTypes);
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

        private object CreateTestInstance(Type test, Type[] ctorArgTypes)
        {
            var ctorArgs = ctorArgTypes.Select(x => typeResolver.ResolveType(x)).ToArray();
            var instance = Activator.CreateInstance(test, ctorArgs);
            if (instance is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
            return instance;
        }
    }
}