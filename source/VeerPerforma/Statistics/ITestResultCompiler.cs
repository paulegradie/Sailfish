using System;
using System.Collections.Generic;
using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public interface ITestResultCompiler
{
    List<CompiledResultContainer> CompileResults(Dictionary<Type, List<TestExecutionResult>> results);
}