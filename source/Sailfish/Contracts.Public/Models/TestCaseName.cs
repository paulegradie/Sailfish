using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

/// <summary>
///     Class to hold and manipulate the test case name Name will be like some.test(maybe:20,other:30)
///     some.other.test(maybe:10,other:30)
/// </summary>
public class TestCaseName
{
    private const char OpenBracket = '(';
    private const char Dot = '.';

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TestCaseName()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    // Deserialization target. Name/Parts are get-only, so the serializer cannot populate them via a
    // parameterless ctor — that left Name null after every tracking-file round-trip, which silently
    // broke ScaleFish (it groups observations by TestCaseName.Name). Binding them through this ctor
    // (params match the serialized "Name"/"Parts") restores a faithful round-trip.
    [JsonConstructor]
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

    public string Name { get; }

    public IReadOnlyList<string> Parts { get; }

    private static string[] GetNameParts(string displayName)
    {
        return displayName.Split(OpenBracket).First().Split(Dot).ToArray();
    }

    public string GetMethodPart()
    {
        return Name.Split(Dot).Last();
    }
}