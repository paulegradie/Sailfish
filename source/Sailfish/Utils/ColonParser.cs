using Sailfish.Extensions.Types;

namespace Sailfish.Utils;

internal static class ColonParser
{
    public static OrderedDictionary Parse(string[]? tags)
    {
        var keyValues = new OrderedDictionary();
        if (tags is null) return keyValues;

        foreach (var tag in tags)
        {
            var keyVal = tag.Split(":");
            if (keyVal.Length != 2) continue;

            var key = keyVal[0];
            var value = keyVal[1];

            if (string.IsNullOrEmpty(key)) continue;
            if (string.IsNullOrEmpty(value)) continue;
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (string.IsNullOrWhiteSpace(value)) continue;

            keyValues.Add(key, value);
        }

        return keyValues;
    }
}