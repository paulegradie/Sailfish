namespace VeerPerforma.Execution;

public interface IInstanceCreator
{
    List<object> CreateInstances(Type test, IEnumerable<IEnumerable<int>> combos, List<string> propNames);
}