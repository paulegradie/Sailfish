using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface ITestCollector
{
    IEnumerable<Type> CollectTestTypes(IEnumerable<Type> sourceTypes);
}