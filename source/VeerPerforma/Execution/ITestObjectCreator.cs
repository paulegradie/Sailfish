using System;
using System.Collections.Generic;

namespace VeerPerforma.Execution
{
    public interface ITestObjectCreator
    {
        List<TestInstanceContainer> CreateTestContainerInstances(Type test);
    }
}