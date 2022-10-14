using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal delegate void AdapterCallbackAction(TestExecutionResult result);

internal interface ISailFishTestExecutor
{
    Task<List<RawExecutionResult>> Execute(IEnumerable<Type> testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default);

    Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null, CancellationToken cancellationToken = default);


    Task<TestExecutionResult> Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default);
}