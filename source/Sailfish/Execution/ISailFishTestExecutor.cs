using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.Execution
{
    internal delegate void AdapterCallbackAction(TestExecutionResult result);

    internal interface ISailFishTestExecutor
    {
        Task<List<RawExecutionResult>> Execute(Type[] testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<List<TestExecutionResult>> Execute(List<TestInstanceContainerProvider> testMethods, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<TestExecutionResult> Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
    }
}