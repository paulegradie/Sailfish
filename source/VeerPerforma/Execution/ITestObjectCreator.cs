namespace VeerPerforma.Execution;

public interface ITestObjectCreator
{
    List<TestInstanceContainer> CreateTestContainerInstances(Type test);
}