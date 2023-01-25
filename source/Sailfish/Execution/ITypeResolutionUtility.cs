using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

public interface ITypeResolutionUtility
{
    object CreateDehydratedTestInstance(Type test, IEnumerable<Type> anchorTypes);
}