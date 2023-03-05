using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        Action<TestInstanceContainer>? preCallback = null,
        Action<TestExecutionResult, TestInstanceContainer>? callback = null,
        Action<TestInstanceContainer?>? exceptionCallback = null,
        CancellationToken cancellationToken = default);
}