using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerCreator
{
    List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type test,
        Func<PropertySet, bool>? filter = null,
        Func<MethodInfo, bool>? methodFilter = null);
}