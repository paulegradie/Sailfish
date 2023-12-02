using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Class to hold and manipulate the test case name
/// Name will be like
/// some.test(maybe:20,other:30)
/// some.other.test(maybe:10,other:30)
///
/// </summary>
public class TestCaseName
{
    private const char OpenBracket = '(';
    private const char Dot = '.';


    [JsonConstructor]
#pragma warning disable CS8618
    public TestCaseName()
#pragma warning restore CS8618
    {
    }

    public TestCaseName(string name, IReadOnlyList<string> parts)
    {
        Name = name;
        Parts = parts;
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

    public string Name { get; set; }
    public IReadOnlyList<string> Parts { get; set; }

    private static IReadOnlyList<string> GetNameParts(string displayName)
    {
        return displayName.Split(OpenBracket).First().Split(Dot).ToArray();
    }

    public string GetMethodPart()
    {
        return Name.Split(Dot).Last();
    }

    /// <summary>
    /// Method to parse and return an index of the '.' delimited test name.
    /// /// e.g. some.test where index 0 = some
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
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