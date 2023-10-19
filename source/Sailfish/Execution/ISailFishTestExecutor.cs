using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailFishTestExecutor
{
    Task<List<TestClassResultGroup>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default);
}