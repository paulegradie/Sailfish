using System;
using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Statistics;

public interface ITestResultCompiler
{
    List<CompiledResultContainer> CompileResults(Dictionary<Type, List<TestExecutionResult>> results);
}