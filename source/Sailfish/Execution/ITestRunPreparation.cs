using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Execution
{
    internal interface ITestRunPreparation
    {
        Dictionary<string, List<(MethodInfo, object)>> GenerateTestInstances(Type test);
    }
}