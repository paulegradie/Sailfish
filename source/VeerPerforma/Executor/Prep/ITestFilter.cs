namespace VeerPerforma.Executor.Prep;

public interface ITestFilter
{
    Task<Type[]> FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
}