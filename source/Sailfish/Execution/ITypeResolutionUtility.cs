using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.Execution;

public interface ITypeResolutionUtility
{
    Task<object> CreateDehydratedTestInstance(Type test, IEnumerable<Type> anchorTypes);
}