﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.Execution
{
    public delegate void AdapterCallbackAction(TestExecutionResult result);

    public interface ISailFishTestExecutor
    {
        Task<Dictionary<Type, List<TestExecutionResult>>> Execute(Type[] testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<List<TestExecutionResult>> Execute(List<TestInstanceContainerProvider> testMethods, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
        Task<TestExecutionResult> Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
    }
}