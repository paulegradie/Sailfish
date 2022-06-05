namespace VeerPerforma.Executor.Prep;

public static class InstanceConstructor
{
    // must be parameterless
    public static IEnumerable<IEnumerable<string>> GetAllPossibleCombos(
        IEnumerable<IEnumerable<string>> strings)
    {
        IEnumerable<IEnumerable<string>> combos = new string[][] { new string[0] };

        foreach (var inner in strings)
            combos = from c in combos
                from i in inner
                select c.Append(i);

        return combos;
    }

    public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource item)
    {
        foreach (var element in source)
            yield return element;

        yield return item;
    }

    public static object CreateInstance(Type type, params int[] args)
    {
        var instance = Activator.CreateInstance(type, args);
        if (instance is null) throw new Exception("Weird program error encountered");
        return instance;
    }
}