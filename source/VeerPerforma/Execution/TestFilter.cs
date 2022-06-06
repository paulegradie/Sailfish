namespace VeerPerforma.Execution;

public class TestFilter : ITestFilter
{
    private readonly ITestListValidator testListValidator;

    public TestFilter(ITestListValidator testListValidator)
    {
        this.testListValidator = testListValidator;
    }

    Type[] FilterTests(Type[] tests, string[] testsRequestedByUser)
    {
        if (testsRequestedByUser.Length > 0)
        {
            return tests
                .Where(test => testsRequestedByUser.Contains(test.Name))
                .ToArray();
        }

        return tests;
    }

    public Type[] FilterAndValidate(Type[] tests, string[] testsRequestedByUser)
    {
        var filtered = FilterTests(tests, testsRequestedByUser);
        var result = testListValidator.ValidateTests(testsRequestedByUser, filtered);
        if (!result.IsValid) throw new Exception("Couldn't find all tests"); // TODO: make use of the result errors
        return filtered;
    }
}