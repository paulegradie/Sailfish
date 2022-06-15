using System.Collections.Generic;
using System.Reflection;

namespace VeerPerforma.Execution
{
    public interface IMethodOrganizer
    {
        Dictionary<string, List<(MethodInfo, object)>> FormMethodGroups(List<object> instances);
    }
}