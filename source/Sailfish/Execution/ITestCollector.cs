using System;

namespace Sailfish.Execution
{
    public interface ITestCollector
    {
        Type[] CollectTestTypes(params Type[] sourceTypes);
        Type[] CollectTestTypes();
    }
}