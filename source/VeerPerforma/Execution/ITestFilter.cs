namespace VeerPerforma.Execution;

public interface ITestFilter
{
    Type[] FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
}