using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailFishTestExecutor
{
    Task<List<RawExecutionResult>> Execute(
        IEnumerable<Type> testTypes,
        Action<TestExecutionResult, TestInstanceContainer>? callback = null,
        CancellationToken cancellationToken = default);
}