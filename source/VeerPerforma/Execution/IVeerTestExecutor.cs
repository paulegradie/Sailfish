namespace VeerPerforma.Execution;

public interface IVeerTestExecutor
{
    Task<int> Execute(Type[] tests, Action<Type, int, int>? callback = null);
    Task Execute(Type test, Action<Type, int, int>? callback = null);
}