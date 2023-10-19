using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        Action<TestInstanceContainer>? preTestCallback = null,
        Action<TestCaseExecutionResult, TestInstanceContainer>? postTestCallback = null,
        Action<TestInstanceContainer?>? exceptionCallback = null,
        Action<TestInstanceContainer?>? testDisabledCallback = null,
        CancellationToken cancellationToken = default);
}