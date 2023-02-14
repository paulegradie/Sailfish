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
        Action<TestInstanceContainerProvider>? preCallback = null,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default);
}