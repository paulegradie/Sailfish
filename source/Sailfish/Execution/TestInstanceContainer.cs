using System;
using System.Reflection;
using Sailfish.Statistics;
using Sailfish.Utils;

namespace Sailfish.Execution
{
    /// <summary>
    /// A test instance container is a single case for a single method for a single type
    /// It will contain all of the necessary items to execute a test case
    /// </summary>
    public class TestInstanceContainer
    {
        private TestInstanceContainer(
            Type type,
            object instance,
            MethodInfo method,
            string displayName,
            ExecutionSettings executionSettings
        )
        {
            Type = type;
            Instance = instance;
            ExecutionMethod = method;
            DisplayName = displayName;
            ExecutionSettings = executionSettings;
            GroupingId = $"{instance.GetType().Name}.{method.Name}";
        }

        public Type Type { get; }

        public object Instance { get; set; }

        public MethodInfo ExecutionMethod { get; }
        public string GroupingId { get; set; }
        public string DisplayName { get; } // This is a uniq id since we take a Distinct on all Iteration Variable attribute param -- class.method(varA: 1, varB: 3) is the form
        public int NumWarmupIterations => ExecutionSettings.NumWarmupIterations;
        public int NumIterations => ExecutionSettings.NumIterations;


        public ExecutionSettings ExecutionSettings { get; }

        public AncillaryInvocation Invocation { get; protected set; } = null!;

        public static TestInstanceContainer CreateTestInstance(object instance, MethodInfo method, string[] propertyNames, int[] variables)
        {
            if (propertyNames.Length != variables.Length) throw new Exception("Property names and variables do not match");

            var paramsDisplay = DisplayNameHelper.CreateParamsDisplay(propertyNames, variables);
            var fullDisplayName = DisplayNameHelper.CreateDisplayName(instance.GetType(), method.Name, paramsDisplay); // a uniq id

            var executionSettings = instance.GetType().RetrieveExecutionTestSettings();

            return new TestInstanceContainer(instance.GetType(), instance, method, fullDisplayName, executionSettings)
            {
                Invocation = new AncillaryInvocation(instance, method, new PerformanceTimer())
            };
        }
    }
}