using System;
using System.Reflection;
using VeerPerforma.Execution;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution
{
    public class TestInstanceContainer
    {
        private TestInstanceContainer(
            Type type,
            object instance,
            MethodInfo method,
            string displayName,
            int numWarmupIterations,
            int numIterations
        )
        {
            Type = type;
            Instance = instance;
            ExecutionMethod = method;
            DisplayName = displayName;
            NumIterations = numIterations;
            NumWarmupIterations = numWarmupIterations;
        }

        public Type Type { get; }
        public object Instance { get; }
        public MethodInfo ExecutionMethod { get; }
        public string DisplayName { get; } // This is a uniq id since we take a Distinct on all Iteration Variable attribute param -- class.method(varA: 1, varB: 3) is the form
        public int NumWarmupIterations { get; }
        public int NumIterations { get; }

        public AncillaryInvocation Invocation => new AncillaryInvocation(Instance, ExecutionMethod, new PerformanceTimer());

        public static TestInstanceContainer CreateTestInstance(object instance, MethodInfo method, string[] propertyNames, int[] variables)
        {
            if (propertyNames.Length != variables.Length) throw new Exception("Property names and variables do not match");

            var paramsDisplay = DisplayNameHelper.CreateParamsDisplay(propertyNames, variables);
            var fullDisplayName = DisplayNameHelper.CreateDisplayName(instance.GetType(), method.Name, paramsDisplay); // a uniq id
            var numWarmupIterations = instance.GetType().GetWarmupIterations();
            var numIterations = instance.GetType().GetNumIterations();

            return new TestInstanceContainer(instance.GetType(), instance, method, fullDisplayName, numWarmupIterations, numIterations);
        }
    }
}