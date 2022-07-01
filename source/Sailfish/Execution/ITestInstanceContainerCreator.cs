using System;
using System.Collections.Generic;

namespace Sailfish.Execution
{
    public interface ITestInstanceContainerCreator
    {
        List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(Type test);
    }
}