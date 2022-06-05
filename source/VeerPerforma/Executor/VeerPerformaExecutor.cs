using VeerPerforma.Executor.Prep;

namespace VeerPerforma.Executor;

/// <summary>
/// This class can be used in a project that is intended to define a bunch of perf tests
/// For example, create a project, and execute this run command. That will allow you to
/// </summary>
public class VeerPerformaExecutor
{
    private readonly ITestExecutor testExecutor;

    public VeerPerformaExecutor(ITestExecutor testExecutor)
    {
        this.testExecutor = testExecutor;
    }

    public async Task<int> Run(string[] testNames)
    {
        // TODO: Call the test executor 
        return await testExecutor.Execute(testNames);
    }
}