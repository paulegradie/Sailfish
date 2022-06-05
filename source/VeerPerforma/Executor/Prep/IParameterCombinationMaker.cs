namespace VeerPerforma.Executor.Prep;

public interface IParameterCombinationMaker
{
    IEnumerable<IEnumerable<int>> GetAllPossibleCombos(
        IEnumerable<IEnumerable<int>> ints);
}