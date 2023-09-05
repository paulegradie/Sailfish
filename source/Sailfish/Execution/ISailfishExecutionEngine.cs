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
        Action<TestInstanceContainer>? preTestCallback = null,
        Action<TestExecutionResult, TestInstanceContainer>? postTestCallback = null,
        Action<TestInstanceContainer?>? exceptionCallback = null,
        Action<TestInstanceContainer?>? testDisabledCallback = null,
        CancellationToken cancellationToken = default);
}