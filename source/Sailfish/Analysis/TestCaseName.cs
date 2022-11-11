using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

public class TestCaseName
{
    // name will be like
    // some.test(maybe:20,other:30)
    // some.other.test(maybe:10,other:30)

    private const char OpenBracket = '(';
    private const char Dot = '.';

    [JsonConstructor]
    public TestCaseName()
    {
    }

    public TestCaseName(string displayName)
    {
        Name = displayName.Split(OpenBracket).First();
        Parts = GetNameParts(Name);
    }

    public TestCaseName(IReadOnlyList<string> parts)
    {
        Name = string.Join(Dot, parts);
        Parts = parts;
    }

    public string Name { get; } = null!;
    public IReadOnlyList<string> Parts { get; } = null!;

    private static IReadOnlyList<string> GetNameParts(string displayName)
    {
        return displayName.Split(OpenBracket).First().Split(Dot).ToArray();
    }

    public string? GetNamePart(int index)
    {
        try
        {
            return GetNameParts(Name)[index];
        }
        catch
        {
            return null;
        }
    }
}