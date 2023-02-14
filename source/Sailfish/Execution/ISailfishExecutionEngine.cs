using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        Action<TestInstanceContainer>? preCallback = null,
        Action<TestExecutionResult, TestInstanceContainer>? callback = null,
        CancellationToken cancellationToken = default);
}