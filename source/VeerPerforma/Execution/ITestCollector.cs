namespace VeerPerforma.Execution;

public interface ITestCollector
{
    Type[] CollectTestTypes(params Type[] sourceTypes);
    Type[] CollectTestTypes();
}