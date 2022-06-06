namespace VeerPerforma.Execution;

public interface IParameterGridCreator
{
    (List<string>, IEnumerable<IEnumerable<int>>) GenerateParameterGrid(Type test);
}