using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Utils;

namespace Sailfish.Execution
{
    public class TestCollector : ITestCollector
    {
        public Type[] CollectTestTypes(params Type[] sourceTypes)
        {
            if (sourceTypes.Length == 0)
                return CollectTestTypes();

            var allTests = new List<Type>();
            foreach (var sourceType in sourceTypes)
            {
                var allTypes = sourceType.Assembly.GetTypes().Where(t => t.HasAttribute<SailfishAttribute>());
                allTests.AddRange(allTypes);
            }

            return allTests.Distinct().ToArray();
        }

        public Type[] CollectTestTypes()
        {
            var types = Assembly.GetCallingAssembly().GetTypes().Where(t => t.HasAttribute<SailfishAttribute>()).ToArray();
            return types;
        }
    }
}