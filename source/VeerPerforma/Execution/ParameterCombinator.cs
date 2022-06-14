namespace VeerPerforma.Execution;

public class ParameterCombinator : IParameterCombinator
{
    public int[][] GetAllPossibleCombos(IEnumerable<IEnumerable<int>> ints)
    {
        var strings = ints.Select(x => x.Select(x => x.ToString()));
        IEnumerable<IEnumerable<string>> combos = new[] { new string[0] };

        foreach (var inner in strings)
            combos = from c in combos
                from i in inner
                select c.Append(i);

        return combos.Select(x => x.Select(int.Parse).ToArray()).ToArray();
    }
}

public static class ParameterCombinatorExtensionMethods
{
    public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource item)
    {
        foreach (var element in source)
            yield return element;

        yield return item;
    }
}