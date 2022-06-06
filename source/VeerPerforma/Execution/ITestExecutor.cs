namespace VeerPerforma.Execution;

public interface ITestExecutor
{
    Task<int> Execute(Type[] tests);
}