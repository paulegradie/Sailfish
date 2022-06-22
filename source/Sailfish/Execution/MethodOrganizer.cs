using System.Collections.Generic;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Utils;

namespace Sailfish.Execution
{
    public class MethodOrganizer : IMethodOrganizer
    {
        public Dictionary<string, List<(MethodInfo, object)>> FormMethodGroups(List<object> instances)
        {
            var methodInstancePairs = new Dictionary<string, List<(MethodInfo, object)>>();

            foreach (var instance in instances)
            {
                var methods = instance.GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>();
                foreach (var method in methods)
                    if (methodInstancePairs.TryGetValue(method.Name, out var methodObjectPairs))
                        methodObjectPairs!.Add((method, instance));
                    else
                        methodInstancePairs.Add(
                            method.Name, new List<(MethodInfo, object)>
                            {
                                (method, instance)
                            });
            }

            return methodInstancePairs;
        }
    }
}