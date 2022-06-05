namespace VeerPerforma.Executor.Prep;

public interface ITestListValidator
{
    TestValidationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames);
}