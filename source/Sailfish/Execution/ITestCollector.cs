using System;

namespace Sailfish.Execution
{
    internal interface ITestCollector
    {
        Type[] CollectTestTypes(params Type[] sourceTypes);
        Type[] CollectTestTypes();
    }
}