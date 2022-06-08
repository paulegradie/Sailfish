using VeerPerforma.Execution;

namespace VeerPerforma;

/// <summary>
/// This class can be used in a project that is intended to define a bunch of perf tests
/// For example, create a project, and execute this run command. That will allow you to
/// </summary>
public class VeerPerformaExecutor
{
    private readonly IVeerTestExecutor veerTestExecutor;
    private readonly ITestCollector testCollector;
    private readonly ITestFilter testFilter;

    public VeerPerformaExecutor(
        IVeerTestExecutor veerTestExecutor,
        ITestCollector testCollector,
        ITestFilter testFilter
    )
    {
        this.veerTestExecutor = veerTestExecutor;
        this.testCollector = testCollector;
        this.testFilter = testFilter;
    }

    public async Task<int> Run(string[] testNames, params Type[] testLocationTypes)
    {
        var tests = CollectTests(testNames, testLocationTypes);
        return await veerTestExecutor.Execute(tests);
    }

    public Type[] CollectTests(string[] testNames, params Type[] locationTypes)
    {
        var perfTests = testCollector.CollectTestTypes(locationTypes);
        return testFilter.FilterAndValidate(perfTests, testNames);
    }
}