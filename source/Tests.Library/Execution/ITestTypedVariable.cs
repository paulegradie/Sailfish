using Sailfish.Contracts.Public.Variables;

namespace Tests.Library.Execution;

public interface ITestTypedVariable : ISailfishVariables<TestTypedVariable, TestTypedVariableProvider>
{
    string Name { get; }
    int Value { get; }
}