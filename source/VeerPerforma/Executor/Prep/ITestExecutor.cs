namespace VeerPerforma.Executor.Prep;

public interface ITestExecutor
{
    Task<int> Execute(string[] testsRequestedByUser);
}