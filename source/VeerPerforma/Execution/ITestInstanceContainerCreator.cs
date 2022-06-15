using System;
using System.Collections.Generic;

namespace VeerPerforma.Execution
{
    public interface ITestInstanceContainerCreator
    {
        List<TestInstanceContainer> CreateTestContainerInstances(Type test);
    }
}