using System.Reflection;

namespace VeerPerforma.Execution;

public delegate void AdapterCallbackAction(TestExecutionResult result);

public interface IVeerTestExecutor
{
    Task<int> Execute(Type[] testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
    Task Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
    Task Execute(List<TestInstanceContainer> testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
    Task Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null);
}