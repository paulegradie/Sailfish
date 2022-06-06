namespace VeerPerforma.Execution;

public interface IParameterCombinator
{
    IEnumerable<IEnumerable<int>> GetAllPossibleCombos(
        IEnumerable<IEnumerable<int>> ints);
}