using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Execution
{
    public interface IMethodOrganizer
    {
        Dictionary<string, List<(MethodInfo, object)>> FormMethodGroups(List<object> instances);
    }
}