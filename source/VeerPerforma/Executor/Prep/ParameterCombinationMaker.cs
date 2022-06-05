namespace VeerPerforma.Executor.Prep;

public class ParameterCombinationMaker : IParameterCombinationMaker
{
    public IEnumerable<IEnumerable<int>> GetAllPossibleCombos(
        IEnumerable<IEnumerable<int>> ints)
    {
        var strings = ints.Select(x => x.Select(x => x.ToString()));
        IEnumerable<IEnumerable<string>> combos = new string[][] { new string[0] };

        foreach (var inner in strings)
            combos = from c in combos
                from i in inner
                select c.Append(i);

        return combos.Select(x => x.Select(x => int.Parse(x)));
    }
}

public static class ParameterCombinationMakerExtensionMethods
{
    public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource item)
    {
        foreach (var element in source)
            yield return element;

        yield return item;
    }
}