namespace VeerPerforma.Execution;

public interface ITestFilter
{
    TestValidationResult FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
}