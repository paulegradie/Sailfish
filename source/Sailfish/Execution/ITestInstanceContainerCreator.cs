using System;
using System.Collections.Generic;

namespace Sailfish.Execution
{
    public interface ITestInstanceContainerCreator
    {
        List<TestInstanceContainer> CreateTestContainerInstances(Type test);
    }
}