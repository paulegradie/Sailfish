using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerCreator
{
    List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(Type test);
}