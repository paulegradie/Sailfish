namespace VeerPerforma.Executor.Prep;

public interface ITestExecutor
{
    Task<int> Execute(string[] testNames);
    Task<int> Execute(string[] testNames, params Type[] locationTypes);
}