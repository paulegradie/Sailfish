namespace VeerPerforma.Executor.Prep;

public interface ITestCollector
{
    Type[] CollectTestTypes(params Type[] sourceTypes);
    Type[] CollectTestTypes();
}